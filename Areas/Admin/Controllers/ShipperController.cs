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
	[Authorize(Roles = "Shipper,Admin")]
	public class ShipperController : Controller
	{
		private readonly AppDbContext _context;

		// [THÊM]: Khai báo service thông báo và quản lý người dùng
		private readonly INotificationService _notificationService;
		private readonly UserManager<ApplicationUser> _userManager;

		// [THÊM]: Inject INotificationService và UserManager vào Constructor
		public ShipperController(
			AppDbContext context,
			INotificationService notificationService,
			UserManager<ApplicationUser> userManager)
		{
			_context = context;
			_notificationService = notificationService;
			_userManager = userManager;
		}

		public async Task<IActionResult> Index() => View(await _context.Orders.Include(o => o.Address)
			.Where(o => o.Status == OrderStatus.Preparing || o.Status == OrderStatus.Delivering).ToListAsync());

		[HttpPost]
		public async Task<IActionResult> PickUp(int orderId) => await ChangeStatus(orderId, OrderStatus.Delivering);

		[HttpPost]
		public async Task<IActionResult> CompleteDelivery(int orderId) => await ChangeStatus(orderId, OrderStatus.Completed);

		private async Task<IActionResult> ChangeStatus(int orderId, OrderStatus to)
		{
			var order = await _context.Orders.FindAsync(orderId);
			if (order != null && OrderStatusMachine.CanTransition(order.Status, to))
			{
				order.Status = to;
				await _context.SaveChangesAsync();

				// =========================================================================
				// [THÊM]: GỬI THÔNG BÁO KHI SHIPPER ĐỔI TRẠNG THÁI GIAO HÀNG
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
			}
			return RedirectToAction("Index");
		}
	}
}