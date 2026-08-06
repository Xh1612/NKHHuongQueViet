using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using HuongQueViet.Models;

namespace HuongQueViet.ViewComponents
{
    public class CartSummaryViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var json = HttpContext.Session.GetString("Cart");
            var cart = json == null ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(json)!;
            return View(cart.Sum(c => c.Quantity));
        }
    }
}