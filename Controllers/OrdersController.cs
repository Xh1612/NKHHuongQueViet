using System.Security.Claims;
using System.Text.Json;
using HuongQueViet.Data;
using HuongQueViet.Helpers;
using HuongQueViet.Models;
using HuongQueViet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class OrdersController : Controller
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly ICouponService _couponService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;
    private const string CartSessionKey = "Cart";

    public OrdersController(AppDbContext context, IConfiguration config, ICouponService couponService,
        UserManager<ApplicationUser> userManager, INotificationService notificationService)
    {
        _context = context;
        _config = config;
        _couponService = couponService;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    private List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        return json == null ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(json)!;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var orders = await _context.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.OrderDate).ToListAsync();
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cart = GetCart();
        if (!cart.Any()) return RedirectToAction("Index", "Cart");
        ViewBag.Addresses = await _context.Addresses
            .Where(a => a.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier)).ToListAsync();
        return View(cart);
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(int addressId, string paymentMethod, string? couponCode)
    {
        var cart = GetCart();
        if (!cart.Any()) return RedirectToAction("Index", "Cart");

        var address = await _context.Addresses.FindAsync(addressId);
        if (address == null)
        {
            TempData["Error"] = "Vui lòng chọn địa chỉ giao hàng";
            return RedirectToAction("Checkout");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var zone = await _context.DeliveryZones.FirstOrDefaultAsync(z => z.Province == address.Province && z.District == address.District);
            var distanceKm = DistanceHelper.CalculateKm(
                _config.GetValue<double>("RestaurantLocation:Lat"), _config.GetValue<double>("RestaurantLocation:Lng"),
                address.Lat, address.Lng);
            decimal shippingFee = zone != null ? zone.BaseFee + zone.FeePerKm * (decimal)distanceKm : 25000;

            var order = new Order
            {
                UserId = userId!,
                AddressId = address.Id,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                PaymentMethod = paymentMethod,
                ShippingFee = Math.Round(shippingFee, 0),
                ETA = DateTime.Now.AddMinutes(45)
            };

            decimal itemsTotal = 0;
            foreach (var item in cart)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null) throw new InvalidOperationException($"Sản phẩm {item.ProductName} không tồn tại");

                var recipe = await _context.ProductIngredients.Include(pi => pi.Ingredient)
                    .Where(pi => pi.ProductId == item.ProductId).ToListAsync();

                if (recipe.Any())
                {
                    // Chỉ KIỂM TRA số lượng nguyên liệu xem có đủ không (Không trừ kho ở đây nữa)
                    foreach (var pi in recipe)
                    {
                        var needed = pi.QuantityNeeded * item.Quantity;
                        if (pi.Ingredient!.StockQuantity < needed)
                            throw new InvalidOperationException($"Nguyên liệu '{pi.Ingredient.Name}' không đủ để làm {item.ProductName}");
                    }
                }
                else
                {
                    // Chỉ KIỂM TRA tồn kho xem có đủ không (Không trừ kho ở đây nữa)
                    if (product.StockQuantity < item.Quantity)
                        throw new InvalidOperationException($"Sản phẩm {item.ProductName} không đủ tồn kho");
                }

                order.OrderItems.Add(new OrderItem { ProductId = item.ProductId, Quantity = item.Quantity, UnitPrice = item.UnitPrice });
                itemsTotal += item.SubTotal;
            }

            decimal discount = 0;
            if (!string.IsNullOrEmpty(couponCode))
            {
                var (isValid, message, discountAmount) = await _couponService.ValidateAndCalculate(couponCode, itemsTotal);
                if (!isValid)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = message;
                    return RedirectToAction("Checkout");
                }
                discount = discountAmount;
                order.CouponCode = couponCode;
                order.DiscountAmount = discount;
            }

            order.TotalAmount = itemsTotal + order.ShippingFee - discount;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            HttpContext.Session.Remove(CartSessionKey);

            try
            {
                var currentUser = await _userManager.FindByIdAsync(userId!);
                var emailToNotify = currentUser?.Email ?? HttpContext.Session.GetString("CurrentUserEmail") ?? User.Identity?.Name;
                var phone = currentUser?.PhoneNumber ?? string.Empty;
                if (!string.IsNullOrEmpty(emailToNotify))
                {
                    // If configured, use user email as From
                    var useUserAsFrom = bool.TryParse(_config["Smtp:UseUserAsFrom"], out var useVal) && useVal;
                    var fromAddress = useUserAsFrom ? emailToNotify : null;
                    await _notificationService.NotifyOrderPlaced(order, emailToNotify, phone, fromAddress);
                }
            }
            catch (Exception notifyEx)
            {
                Console.WriteLine($"[Cảnh báo] Gửi thông báo cho đơn #{order.Id} thất bại: {notifyEx.Message}");
            }

            if (paymentMethod == "VNPay") return RedirectToAction("Create", "Payment", new { orderId = order.Id });
            return RedirectToAction("Confirmation", new { id = order.Id });
        }
        catch (Exception ex)
        {
            try { await transaction.RollbackAsync(); } catch { /* bỏ qua lỗi rollback */ }
            TempData["Error"] = ex.Message;
            return RedirectToAction("Checkout");
        }
    }

    public async Task<IActionResult> Confirmation(int id)
    {
        var order = await _context.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Product).FirstOrDefaultAsync(o => o.Id == id);
        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        if (!OrderStatusMachine.CanTransition(order.Status, OrderStatus.Cancelled))
        {
            TempData["Error"] = "Đơn hàng đã được chuẩn bị/giao, không thể hủy";
            return RedirectToAction("Confirmation", new { id });
        }

        // Hoàn lại kho nếu khách tự hủy đơn sau khi đã thanh toán VNPay thành công
        if (order.IsPaid)
        {
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                }
            }
        }

        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync();
        return RedirectToAction("Confirmation", new { id });
    }
}