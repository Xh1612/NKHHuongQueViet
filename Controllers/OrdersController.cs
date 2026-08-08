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
                    foreach (var pi in recipe)
                    {
                        var needed = pi.QuantityNeeded * item.Quantity;
                        if (pi.Ingredient!.StockQuantity < needed)
                            throw new InvalidOperationException($"Nguyên liệu '{pi.Ingredient.Name}' không đủ để làm {item.ProductName}");
                    }
                    foreach (var pi in recipe)
                    {
                        var used = pi.QuantityNeeded * item.Quantity;
                        pi.Ingredient!.StockQuantity -= used;
                        _context.InventoryLogs.Add(new InventoryLog { IngredientId = pi.IngredientId, Change = -used, Reason = $"Chế biến {item.ProductName}" });
                    }
                }
                else
                {
                    if (product.StockQuantity < item.Quantity) throw new InvalidOperationException($"Sản phẩm {item.ProductName} không đủ tồn kho");
                    product.StockQuantity -= item.Quantity;
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

            // Đơn đã commit xong ở trên rồi — lỗi gửi thông báo (email sai định dạng, SMTP chưa cấu hình...)
            // chỉ ghi log, KHÔNG được ném lại, nếu không sẽ làm đơn đã thành công bị báo nhầm là thất bại.
            try
            {
                var currentUser = await _userManager.FindByIdAsync(userId!);
                if (currentUser != null)
                {
                    await _notificationService.NotifyOrderPlaced(order, currentUser.Email!, currentUser.PhoneNumber ?? "");
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
            try { await transaction.RollbackAsync(); } catch { /* transaction có thể đã tự đóng do lỗi trước đó, bỏ qua */ }
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
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();
        if (!OrderStatusMachine.CanTransition(order.Status, OrderStatus.Cancelled))
        {
            TempData["Error"] = "Đơn hàng đã được chuẩn bị/giao, không thể hủy";
            return RedirectToAction("Confirmation", new { id });
        }
        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync();
        return RedirectToAction("Confirmation", new { id });
    }
}