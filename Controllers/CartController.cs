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

        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();
            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.ProductId == productId);
            if (existing != null) existing.Quantity += quantity;
            else cart.Add(new CartItem { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = quantity });
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Remove(int productId)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.ProductId == productId);
            SaveCart(cart);
            return RedirectToAction("Index");
        }
    }
}