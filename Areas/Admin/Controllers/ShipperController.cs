using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuongQueViet.Data;
using HuongQueViet.Helpers;
using HuongQueViet.Models;

namespace HuongQueViet.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Shipper,Admin")]
    public class ShipperController : Controller
    {
        private readonly AppDbContext _context;
        public ShipperController(AppDbContext context) { _context = context; }

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
            }
            return RedirectToAction("Index");
        }
    }
}