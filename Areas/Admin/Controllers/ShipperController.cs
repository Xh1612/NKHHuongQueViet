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