using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuongQueViet.Data;
using HuongQueViet.Models;

namespace HuongQueViet.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private const string CartSessionKey = "Cart";
        public CartController(AppDbContext context) { _context = context; }

        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);
            return json == null ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(json)!;
        }

        private void SaveCart(List<CartItem> cart) => HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));

        public IActionResult Index() => View(GetCart());

        // API lấy tổng số lượng sản phẩm trong giỏ hàng
        [HttpGet]
        public IActionResult GetCartCount()
        {
            var cart = GetCart();
            int count = cart.Sum(c => c.Quantity);
            return Json(new { count = count });
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();
            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.ProductId == productId);
            if (existing != null) existing.Quantity += quantity;
            else cart.Add(new CartItem { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = quantity });
            SaveCart(cart);

            int totalCount = cart.Sum(c => c.Quantity);

            // Trả về JSON nếu là request AJAX/Fetch
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { count = totalCount });
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Remove(int productId)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.ProductId == productId);
            SaveCart(cart);

            int totalCount = cart.Sum(c => c.Quantity);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { count = totalCount });
            }

            return RedirectToAction("Index");
        }

        // Action hỗ trợ xóa sạch giỏ hàng
        [HttpPost]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CartSessionKey);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { count = 0 });
            }

            return RedirectToAction("Index");
        }
    }
}