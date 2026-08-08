using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuongQueViet.Data;
using HuongQueViet.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace HuongQueViet.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        public ProductsController(AppDbContext context, IWebHostEnvironment env) { _context = context; _env = env; }

        public async Task<IActionResult> Index() => View(await _context.Products.Include(p => p.Category).OrderBy(p => p.Id).ToListAsync());

        public IActionResult Create()
        {
            //ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (imageFile != null && imageFile.Length > 0) product.ImageUrl = await SaveImage(imageFile);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            //ViewBag.Categories = _context.Categories.ToList();

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile)
        {
            if (id != product.Id) return NotFound();
            if (imageFile != null && imageFile.Length > 0) product.ImageUrl = await SaveImage(imageFile);
            else
            {
                var existing = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                product.ImageUrl = existing?.ImageUrl;
            }
            _context.Update(product);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            if (await _context.OrderItems.AnyAsync(oi => oi.ProductId == id))
            {
                product.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["Info"] = "Sản phẩm đã có trong đơn hàng cũ nên chỉ được ẩn, không xóa hẳn.";
            }
            else
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        private async Task<string> SaveImage(IFormFile file)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) throw new InvalidOperationException("Chỉ chấp nhận ảnh JPG/PNG/WEBP");

            var fileName = $"{Guid.NewGuid()}{ext}";
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);
            using var stream = new FileStream(Path.Combine(uploadsFolder, fileName), FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/{fileName}";
        }
    }
}