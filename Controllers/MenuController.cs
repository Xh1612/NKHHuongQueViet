using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuongQueViet.Data;

namespace HuongQueViet.Controllers
{
    public class MenuController : Controller
    {
        private readonly AppDbContext _context;
        public MenuController(AppDbContext context) { _context = context; }

        public async Task<IActionResult> Index(int? categoryId, string? keyword)
        {
            var query = _context.Products.Where(p => p.IsActive);
            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId);
            if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(p => p.Name.Contains(keyword));

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Keyword = keyword;

            // --- BỔ SUNG DÒNG NÀY ĐỂ TRUYỀN ID RA VIEW ---
            ViewBag.CategoryId = categoryId;

            return View(await query.ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> AdvancedSearch(string? keyword, int? categoryId, decimal? minPrice, decimal? maxPrice, bool? isSpicy)
        {
            var query = _context.Products.Where(p => p.IsActive);
            if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(p => p.Name.Contains(keyword) || p.Description.Contains(keyword));
            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId);
            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice);
            if (isSpicy.HasValue) query = query.Where(p => p.IsSpicy == isSpicy);

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(await query.ToListAsync());
        }
    }
}