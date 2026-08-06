using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuongQueViet.Data;
using HuongQueViet.Models;

namespace HuongQueViet.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StatisticsController : Controller
    {
        private readonly AppDbContext _context;
        public StatisticsController(AppDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var completed = _context.Orders.Where(o => o.Status == OrderStatus.Completed);
            var vm = new StatisticsViewModel
            {
                TotalRevenue = await completed.SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
                TotalOrders = await _context.Orders.CountAsync(),
                OrdersByStatus = await _context.Orders.GroupBy(o => o.Status)
                    .Select(g => new StatusCount { Status = g.Key.ToString(), Count = g.Count() }).ToListAsync(),
                RevenueByDay = await completed.Where(o => o.OrderDate >= DateTime.Now.AddDays(-7))
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new DailyRevenue { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                    .OrderBy(x => x.Date).ToListAsync(),
                TopProducts = await _context.OrderItems.GroupBy(oi => new { oi.ProductId, oi.Product!.Name })
                    .Select(g => new ProductSales { ProductName = g.Key.Name, QuantitySold = g.Sum(oi => oi.Quantity) })
                    .OrderByDescending(p => p.QuantitySold).Take(5).ToListAsync()
            };
            return View(vm);
        }
    }
}