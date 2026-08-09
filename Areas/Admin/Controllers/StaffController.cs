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
using System;
using System.Linq;
using System.Threading.Tasks;

[Area("Admin")]
[Authorize(Roles = "Staff,Admin")]
public class StaffController : Controller
{
    private readonly AppDbContext _context;
    private readonly IHubContext<OrderStatusHub> _hub;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;

    public StaffController(AppDbContext context, IHubContext<OrderStatusHub> hub,
        UserManager<ApplicationUser> userManager, INotificationService notificationService)
    {
        _context = context;
        _hub = hub;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> History()
    {
        var orders = await _context.Orders
            .Include(o => o.Address)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Cancelled)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var userIds = orders.Select(o => o.UserId).Distinct().ToList();
        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);

        var vm = orders.Select(o =>
        {
            users.TryGetValue(o.UserId, out var user);
            return new StaffOrderViewModel
            {
                Id = o.Id,
                Status = o.Status,
                OrderDate = o.OrderDate,

                CustomerName = user?.FullName ?? user?.UserName ?? "(Không rõ)",
                CustomerPhone = user?.PhoneNumber ?? "",

                Street = o.Address?.Street ?? "",
                Ward = o.Address?.Ward ?? "",
                District = o.Address?.District ?? "",
                Province = o.Address?.Province ?? "",

                TotalAmount = o.TotalAmount,
                ShippingFee = o.ShippingFee,
                DiscountAmount = o.DiscountAmount,
                PaymentMethod = o.PaymentMethod,
                IsPaid = o.IsPaid,
                CouponCode = o.CouponCode,

                Items = o.OrderItems.Select(oi => new StaffOrderItemViewModel
                {
                    ProductName = oi.Product?.Name ?? "(Sản phẩm đã xóa)",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };
        }).ToList();

        return View(vm);
    }

    public async Task<IActionResult> Index()
    {
        var orders = await _context.Orders
            .Include(o => o.Address)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Preparing)
            .OrderBy(o => o.OrderDate)
            .ToListAsync();

        var userIds = orders.Select(o => o.UserId).Distinct().ToList();
        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);

        var vm = orders.Select(o =>
        {
            users.TryGetValue(o.UserId, out var user);
            return new StaffOrderViewModel
            {
                Id = o.Id,
                Status = o.Status,
                OrderDate = o.OrderDate,

                CustomerName = user?.FullName ?? user?.UserName ?? "(Không rõ)",
                CustomerPhone = user?.PhoneNumber ?? "",

                Street = o.Address?.Street ?? "",
                Ward = o.Address?.Ward ?? "",
                District = o.Address?.District ?? "",
                Province = o.Address?.Province ?? "",

                TotalAmount = o.TotalAmount,
                ShippingFee = o.ShippingFee,
                DiscountAmount = o.DiscountAmount,
                PaymentMethod = o.PaymentMethod,
                IsPaid = o.IsPaid,
                CouponCode = o.CouponCode,

                Items = o.OrderItems.Select(oi => new StaffOrderItemViewModel
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
    public async Task<IActionResult> Advance(int orderId, OrderStatus toStatus)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return NotFound();

        if (!OrderStatusMachine.CanTransition(order.Status, toStatus))
        {
            TempData["Error"] = $"Không thể chuyển từ {order.Status} sang {toStatus}";
            return RedirectToAction("Index");
        }

        // 1. Trừ kho khi chuyển từ Pending sang Confirmed (Cho đơn COD)
        if (toStatus == OrderStatus.Confirmed && order.Status == OrderStatus.Pending)
        {
            if (order.PaymentMethod != "VNPay" || !order.IsPaid)
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= item.Quantity;
                        if (product.StockQuantity < 0) product.StockQuantity = 0;
                    }
                }
            }
        }

        // 2. Hoàn kho khi đơn bị hủy
        if (toStatus == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
        {
            if (order.IsPaid || order.Status == OrderStatus.Confirmed || order.Status == OrderStatus.Preparing)
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
        }

        order.Status = toStatus;
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

        return RedirectToAction("Index");
    }
}