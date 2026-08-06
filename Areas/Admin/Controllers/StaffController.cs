using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuongQueViet.Data;
using HuongQueViet.Helpers;
using HuongQueViet.Models;

namespace HuongQueViet.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Staff,Admin")]
    public class StaffController : Controller
    {
        private readonly AppDbContext _context;
        public StaffController(AppDbContext context) { _context = context; }

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
            return RedirectToAction("Index");
        }
    }
}