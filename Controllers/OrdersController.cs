using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // [THÊM] Cần thiết để quản lý và tìm thông tin User
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuongQueViet.Data;
using HuongQueViet.Helpers;
using HuongQueViet.Models;
using HuongQueViet.Services; // [Thêm] Cần thiết để gọi dịch vụ gửi thông báo INotificationService

namespace HuongQueViet.Controllers
{
	[Authorize]
	public class OrdersController : Controller
	{
		private readonly AppDbContext _context;
		private readonly IConfiguration _config;

		// [THÊM]: Khai báo 2 biến private để chứa dịch vụ thông báo và quản lý người dùng
		private readonly INotificationService _notificationService;
		private readonly UserManager<ApplicationUser> _userManager;

		private const string CartSessionKey = "Cart";

		// [THÊM]: Inject thêm INotificationService và UserManager<ApplicationUser> vào Constructor
		public OrdersController(
			AppDbContext context,
			IConfiguration config,
			INotificationService notificationService, // [THÊM]
			UserManager<ApplicationUser> userManager)   // [THÊM]
		{
			_context = context;
			_config = config;
			_notificationService = notificationService; // [THÊM]
			_userManager = userManager;                 // [THÊM]
		}

		private List<CartItem> GetCart()
		{
			var json = HttpContext.Session.GetString(CartSessionKey);
			return json == null ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(json)!;
		}

		public async Task<IActionResult> Index()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			return View(await _context.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.OrderDate).ToListAsync());
		}

		[HttpGet]
		public async Task<IActionResult> Checkout()
		{
			var cart = GetCart();
			if (!cart.Any()) return RedirectToAction("Index", "Cart");
			ViewBag.Addresses = await _context.Addresses.Where(a => a.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier)).ToListAsync();
			return View(cart);
		}

		[HttpPost]
		public async Task<IActionResult> Checkout(int addressId, string paymentMethod)
		{
			var cart = GetCart();
			if (!cart.Any()) return RedirectToAction("Index", "Cart");

			var address = await _context.Addresses.FindAsync(addressId);
			if (address == null) { ModelState.AddModelError("", "Vui lòng chọn địa chỉ giao hàng"); return RedirectToAction("Checkout"); }

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
					if (product == null || product.StockQuantity < item.Quantity)
						throw new InvalidOperationException($"Sản phẩm {item.ProductName} không đủ tồn kho");

					product.StockQuantity -= item.Quantity;
					order.OrderItems.Add(new OrderItem { ProductId = item.ProductId, Quantity = item.Quantity, UnitPrice = item.UnitPrice });
					itemsTotal += item.SubTotal;
				}
				order.TotalAmount = itemsTotal + order.ShippingFee;

				_context.Orders.Add(order);
				await _context.SaveChangesAsync();
				await transaction.CommitAsync();

				// Xóa giỏ hàng sau khi đặt thành công
				HttpContext.Session.Remove(CartSessionKey);

				// =========================================================================
				// [THÊM]: TỰ ĐỘNG GỬI THÔNG BÁO EMAIL & MOCK SMS KHI TẠO ĐƠN THÀNH CÔNG
				// =========================================================================
				var currentUser = await _userManager.FindByIdAsync(userId!);
				if (currentUser != null && !string.IsNullOrEmpty(currentUser.Email))
				{
					await _notificationService.NotifyOrderPlaced(
						order,
						currentUser.Email,
						currentUser.PhoneNumber ?? "0900000000"
					);
				}
				// =========================================================================

				return RedirectToAction("Confirmation", new { id = order.Id });
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				ModelState.AddModelError("", ex.Message);
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
			if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
			{
				TempData["Error"] = "Đơn hàng đã được chuẩn bị/giao, không thể hủy";
				return RedirectToAction("Confirmation", new { id });
			}
			order.Status = OrderStatus.Cancelled;
			await _context.SaveChangesAsync();
			return RedirectToAction("Confirmation", new { id });
		}
	}
}