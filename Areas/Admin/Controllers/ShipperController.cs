//using HuongQueViet.Data;
//using HuongQueViet.Helpers;
//using HuongQueViet.Hubs;
//using HuongQueViet.Models;
//using HuongQueViet.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.SignalR;
//using Microsoft.EntityFrameworkCore;

//[Area("Admin")]
//[Authorize(Roles = "Shipper,Admin")]
//public class ShipperController : Controller
//{
//    private readonly AppDbContext _context;
//    private readonly IHubContext<OrderStatusHub> _hub;
//    private readonly UserManager<ApplicationUser> _userManager;
//    private readonly INotificationService _notificationService;

//    public ShipperController(AppDbContext context, IHubContext<OrderStatusHub> hub,
//        UserManager<ApplicationUser> userManager, INotificationService notificationService)
//    {
//        _context = context; _hub = hub; _userManager = userManager; _notificationService = notificationService;
//    }

//    public async Task<IActionResult> Index() => View(await _context.Orders.Include(o => o.Address)
//        .Where(o => o.Status == OrderStatus.Preparing || o.Status == OrderStatus.Delivering).ToListAsync());

//    [HttpPost]
//    public async Task<IActionResult> PickUp(int orderId) => await ChangeStatus(orderId, OrderStatus.Delivering);

//    [HttpPost]
//    public async Task<IActionResult> CompleteDelivery(int orderId) => await ChangeStatus(orderId, OrderStatus.Completed);

//    private async Task<IActionResult> ChangeStatus(int orderId, OrderStatus to)
//    {
//        var order = await _context.Orders.FindAsync(orderId);
//        if (order != null && OrderStatusMachine.CanTransition(order.Status, to))
//        {
//            order.Status = to;
//            await _context.SaveChangesAsync();
//            await _hub.Clients.Group($"order-{order.Id}").SendAsync("StatusUpdated", order.Status.ToString());

//            try
//            {
//                var user = await _userManager.FindByIdAsync(order.UserId);
//                if (user != null)
//                {
//                    await _notificationService.NotifyStatusChanged(order, user.Email!, user.PhoneNumber ?? "");
//                }
//            }
//            catch (Exception notifyEx)
//            {
//                Console.WriteLine($"[Cảnh báo] Gửi thông báo cho đơn #{order.Id} thất bại: {notifyEx.Message}");
//            }
//        }
//        return RedirectToAction("Index");
//    }
//}

using HuongQueViet.Data;
using HuongQueViet.Helpers;
using HuongQueViet.Hubs;
using HuongQueViet.Models;
using HuongQueViet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

[Area("Admin")]
[Authorize(Roles = "Shipper,Admin")]
public class ShipperController : Controller
{
    private readonly AppDbContext _context;
    private readonly IHubContext<OrderStatusHub> _hub;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;

    public ShipperController(AppDbContext context, IHubContext<OrderStatusHub> hub,
        UserManager<ApplicationUser> userManager, INotificationService notificationService)
    {
        _context = context; _hub = hub; _userManager = userManager; _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        var orders = await _context.Orders
            .Include(o => o.Address)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Where(o => o.Status == OrderStatus.Preparing || o.Status == OrderStatus.Delivering)
            .OrderBy(o => o.OrderDate)
            .ToListAsync();

        // Lấy thông tin khách hàng theo lô, tránh N+1 query
        var userIds = orders.Select(o => o.UserId).Distinct().ToList();
        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);

        var vm = orders.Select(o =>
        {
            users.TryGetValue(o.UserId, out var user);
            return new ShipperOrderViewModel
            {
                Id = o.Id,
                Status = o.Status,
                OrderDate = o.OrderDate,
                ETA = o.ETA,

                ReceiverName = user?.FullName ?? user?.UserName ?? "(Không rõ)",
                ReceiverPhone = user?.PhoneNumber ?? "",

                Street = o.Address?.Street ?? "",
                Ward = o.Address?.Ward ?? "",
                District = o.Address?.District ?? "",
                Province = o.Address?.Province ?? "",
                Lat = o.Address?.Lat ?? 0,
                Lng = o.Address?.Lng ?? 0,

                TotalAmount = o.TotalAmount,
                ShippingFee = o.ShippingFee,
                DiscountAmount = o.DiscountAmount,
                PaymentMethod = o.PaymentMethod,
                IsPaid = o.IsPaid,
                CouponCode = o.CouponCode,

                Items = o.OrderItems.Select(oi => new ShipperOrderItemViewModel
                {
                    ProductName = oi.Product?.Name ?? "(Sản phẩm đã xóa)",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };
        }).ToList();

        return View(vm);
    }

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
            await _hub.Clients.Group($"order-{order.Id}").SendAsync("StatusUpdated", order.Status.ToString());
            try
            {
                var user = await _userManager.FindByIdAsync(order.UserId);
                if (user != null)
                {
                    await _notificationService.NotifyStatusChanged(order, user.Email!, user.PhoneNumber ?? "");
                }
            }
            catch (Exception notifyEx)
            {
                Console.WriteLine($"[Cảnh báo] Gửi thông báo cho đơn #{order.Id} thất bại: {notifyEx.Message}");
            }
        }
        return RedirectToAction("Index");
    }
}