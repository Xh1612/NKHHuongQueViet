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

namespace HuongQueViet.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Shipper,Admin")]
    public class ShipperController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<OrderStatusHub> _hub;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IWebHostEnvironment _environment;

        public ShipperController(
            AppDbContext context,
            IHubContext<OrderStatusHub> hub,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _hub = hub;
            _userManager = userManager;
            _notificationService = notificationService;
            _environment = environment;
        }

        // GET: /Admin/Shipper/History
        public async Task<IActionResult> History()
        {
            var orders = await _context.Orders
                .Include(o => o.Address)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Cancelled)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var vm = await MapToViewModelAsync(orders);
            return View(vm);
        }

        // GET: /Admin/Shipper
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            // 1. Tính toán thống kê trong ngày cho Shipper
            var completedToday = await _context.Orders
                .Where(o => o.Status == OrderStatus.Completed && o.OrderDate >= today)
                .ToListAsync();

            ViewBag.TodayCompletedCount = completedToday.Count;
            ViewBag.TodayTotalCOD = completedToday.Where(o => o.PaymentMethod == "COD").Sum(o => o.TotalAmount);
            ViewBag.TodayTotalShippingFee = completedToday.Sum(o => o.ShippingFee);

            // 2. Lấy danh sách các đơn hàng đang chờ giao
            var orders = await _context.Orders
                .Include(o => o.Address)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Where(o => o.Status == OrderStatus.Preparing || o.Status == OrderStatus.Delivering)
                .OrderBy(o => o.OrderDate)
                .ToListAsync();

            var vm = await MapToViewModelAsync(orders);
            return View(vm);
        }

        // Action: Nhận đơn hàng giao
        [HttpPost]
        public async Task<IActionResult> PickUp(int orderId)
            => await ChangeStatus(orderId, OrderStatus.Delivering);

        // Action: Giao hàng thành công (kèm tải ảnh chứng minh)
        [HttpPost]
        public async Task<IActionResult> CompleteDelivery(int orderId, IFormFile? proofImage)
            => await ChangeStatus(orderId, OrderStatus.Completed, proofImage: proofImage);

        // Action: Báo giao hàng thất bại
        [HttpPost]
        public async Task<IActionResult> FailDelivery(int orderId, string? failureReason)
            => await ChangeStatus(orderId, OrderStatus.Cancelled, failureReason: failureReason);

        private async Task<IActionResult> ChangeStatus(int orderId, OrderStatus to, string? failureReason = null, IFormFile? proofImage = null)
        {
            var order = await _context.Orders.FindAsync(orderId);

            // Cho phép chuyển đổi nếu hợp lệ theo StateMachine HOẶC cưỡng chế chuyển sang Cancelled khi báo giao thất bại
            bool canChange = order != null && (OrderStatusMachine.CanTransition(order.Status, to) || to == OrderStatus.Cancelled);

            if (canChange && order != null)
            {
                // Xử lý khi báo giao thất bại
                if (to == OrderStatus.Cancelled)
                {
                    order.FailureReason = string.IsNullOrEmpty(failureReason) ? "Khách không nghe máy / Không nhận hàng" : failureReason;
                }

                // Xử lý lưu ảnh bằng chứng khi giao thành công
                if (to == OrderStatus.Completed)
                {
                    order.IsPaid = true; // Thu tiền thành công

                    if (proofImage != null && proofImage.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "proofs");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string uniqueFileName = $"proof_order_{order.Id}_{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(proofImage.FileName)}";
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await proofImage.CopyToAsync(fileStream);
                        }

                        order.ProofImageUrl = $"/uploads/proofs/{uniqueFileName}";
                    }
                }

                order.Status = to;
                await _context.SaveChangesAsync();

                // Gửi SignalR cập nhật Realtime
                try
                {
                    await _hub.Clients.Group($"order-{order.Id}").SendAsync("StatusUpdated", order.Status.ToString());
                }
                catch { }

                // Gửi thông báo Email/SMS
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

        // Hàm Helper Map dữ liệu sang ViewModel
        private async Task<List<ShipperOrderViewModel>> MapToViewModelAsync(List<Order> orders)
        {
            var userIds = orders.Select(o => o.UserId).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            return orders.Select(o =>
            {
                users.TryGetValue(o.UserId, out var user);

                string phone = !string.IsNullOrEmpty(o.Address?.PhoneNumber)
                    ? o.Address.PhoneNumber
                    : (user?.PhoneNumber ?? "");

                return new ShipperOrderViewModel
                {
                    Id = o.Id,
                    Status = o.Status,
                    OrderDate = o.OrderDate,
                    ETA = o.ETA,

                    ReceiverName = user?.FullName ?? user?.UserName ?? "(Không rõ)",
                    ReceiverPhone = phone,

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
                    FailureReason = o.FailureReason,

                    Items = o.OrderItems.Select(oi => new ShipperOrderItemViewModel
                    {
                        ProductName = oi.Product?.Name ?? "(Sản phẩm đã xóa)",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    }).ToList()
                };
            }).ToList();
        }
    }
}