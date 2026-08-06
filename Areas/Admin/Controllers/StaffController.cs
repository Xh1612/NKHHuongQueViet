using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // [THÊM MỚI - PHẦN 26]: Cần thiết cho UserManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuongQueViet.Data;
using HuongQueViet.Helpers;
using HuongQueViet.Models;
using HuongQueViet.Services; // [THÊM MỚI - PHẦN 26]: Cần thiết cho INotificationService

namespace HuongQueViet.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Staff,Admin")]
	public class StaffController : Controller
	{
		private readonly AppDbContext _context;

		// [THÊM]: Khai báo service thông báo và quản lý người dùng
		private readonly INotificationService _notificationService;
		private readonly UserManager<ApplicationUser> _userManager;

		// [THÊM]: Inject INotificationService và UserManager vào Constructor
		public StaffController(
			AppDbContext context,
			INotificationService notificationService,
			UserManager<ApplicationUser> userManager)
		{
			_context = context;
			_notificationService = notificationService;
			_userManager = userManager;
		}

		public async Task<IActionResult> Index() => View(await _context.Orders
			.Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
			.Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Preparing)
			.OrderBy(o => o.OrderDate).ToListAsync());

		[HttpPost]
		public async Task<IActionResult> Advance(int orderId, OrderStatus toStatus)
		{
			var order = await _context.Orders.FindAsync(orderId);
			if (order == null) return NotFound();
			if (!OrderStatusMachine.CanTransition(order.Status, toStatus))
			{
				TempData["Error"] = $"Không thể chuyển từ {order.Status} sang {toStatus}";
				return RedirectToAction("Index");
			}

			order.Status = toStatus;
			await _context.SaveChangesAsync();

			// =========================================================================
			// [THÊM]: GỬI THÔNG BÁO KHI NHÂN VIÊN ĐỔI TRẠNG THÁI ĐƠN HÀNG
			// =========================================================================
			var customer = await _userManager.FindByIdAsync(order.UserId);
			if (customer != null && !string.IsNullOrEmpty(customer.Email))
			{
				await _notificationService.NotifyStatusChanged(
					order,
					customer.Email,
					customer.PhoneNumber ?? "0900000000"
				);
			}
			// =========================================================================

			return RedirectToAction("Index");
		}
	}
}