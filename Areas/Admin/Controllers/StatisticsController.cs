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

        //public async Task<IActionResult> Index()
        //{
        //    var completed = _context.Orders.Where(o => o.Status == OrderStatus.Completed);
        //    var vm = new StatisticsViewModel
        //    {
        //        TotalRevenue = await completed.SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
        //        TotalOrders = await _context.Orders.CountAsync(),
        //        OrdersByStatus = await _context.Orders.GroupBy(o => o.Status)
        //            .Select(g => new StatusCount { Status = g.Key.ToString(), Count = g.Count() }).ToListAsync(),
        //        RevenueByDay = await completed.Where(o => o.OrderDate >= DateTime.Now.AddDays(-7))
        //            .GroupBy(o => o.OrderDate.Date)
        //            .Select(g => new DailyRevenue { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
        //            .OrderBy(x => x.Date).ToListAsync(),
        //        TopProducts = await _context.OrderItems.GroupBy(oi => new { oi.ProductId, oi.Product!.Name })
        //            .Select(g => new ProductSales { ProductName = g.Key.Name, QuantitySold = g.Sum(oi => oi.Quantity) })
        //            .OrderByDescending(p => p.QuantitySold).Take(5).ToListAsync()
        //    };
        //    return View(vm);
        //}

        public async Task<IActionResult> Index()
        {
            var completed = _context.Orders.Where(o => o.Status == OrderStatus.Completed);

            // 1. Tạo danh sách chuẩn 7 ngày gần nhất (từ 00:00:00)
            var startDate = DateTime.Today.AddDays(-6); // 6 ngày trước + hôm nay = 7 ngày
            var last7Days = Enumerable.Range(0, 7)
                                      .Select(i => startDate.AddDays(i))
                                      .ToList();

            // 2. Lấy dữ liệu thô từ Database
            var rawRevenue = await completed
                .Where(o => o.OrderDate >= startDate)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            var vm = new StatisticsViewModel
            {
                TotalRevenue = await completed.SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
                TotalOrders = await _context.Orders.CountAsync(),
                OrdersByStatus = await _context.Orders.GroupBy(o => o.Status)
                    .Select(g => new StatusCount { Status = g.Key.ToString(), Count = g.Count() }).ToListAsync(),

                // 3. Left Join dữ liệu DB vào danh sách 7 ngày để lấp đầy các ngày 0đ
                RevenueByDay = last7Days.Select(date => new DailyRevenue
                {
                    Date = date,
                    Revenue = rawRevenue.FirstOrDefault(r => r.Date == date)?.Revenue ?? 0
                }).ToList(), // Không cần OrderBy nữa vì last7Days đã theo đúng thứ tự

                TopProducts = await _context.OrderItems.GroupBy(oi => new { oi.ProductId, oi.Product!.Name })
                    .Select(g => new ProductSales { ProductName = g.Key.Name, QuantitySold = g.Sum(oi => oi.Quantity) })
                    .OrderByDescending(p => p.QuantitySold).Take(5).ToListAsync()
            };

            return View(vm);
        }
    }
}