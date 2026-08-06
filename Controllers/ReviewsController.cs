using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuongQueViet.Data;
using HuongQueViet.Models;

namespace HuongQueViet.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly AppDbContext _context;
        public ReviewsController(AppDbContext context) { _context = context; }

        [HttpPost]
        public async Task<IActionResult> Create(int productId, int rating, string comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var purchased = await _context.Orders.Where(o => o.UserId == userId && o.Status == OrderStatus.Completed)
                .SelectMany(o => o.OrderItems).AnyAsync(oi => oi.ProductId == productId);
            if (!purchased)
            {
                TempData["Error"] = "Bạn cần mua và nhận sản phẩm này trước khi đánh giá";
                return RedirectToAction("Details", "Products", new { id = productId });
            }
            if (await _context.Reviews.AnyAsync(r => r.UserId == userId && r.ProductId == productId))
            {
                TempData["Error"] = "Bạn đã đánh giá sản phẩm này rồi";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            _context.Reviews.Add(new Review { ProductId = productId, UserId = userId, Rating = rating, Comment = comment });
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Products", new { id = productId });
        }
    }
}