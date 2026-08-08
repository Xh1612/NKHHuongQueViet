using Microsoft.AspNetCore.Mvc;
using HuongQueViet.Models;
using System.Collections.Generic;
using System.Linq;

namespace HuongQueViet.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class IngredientsController : Controller
    {
        // Tạo một danh sách static để lưu dữ liệu tạm thời không bị mất khi chuyển trang
        private static List<Ingredient> _ingredients = new List<Ingredient>
        {
            new Ingredient { Id = 1, Name = "Thịt bò", Unit = "kg", StockQuantity = 10, LowStockThreshold = 2 },
            new Ingredient { Id = 2, Name = "Bánh phở", Unit = "kg", StockQuantity = 20, LowStockThreshold = 5 },
            new Ingredient { Id = 3, Name = "Hành tây", Unit = "kg", StockQuantity = 3, LowStockThreshold = 4 }
        };

        public IActionResult Index()
        {
            return View(_ingredients);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Ingredient ingredient)
        {
            if (ModelState.IsValid)
            {
                // Tự động tăng ID dựa trên số lượng phần tử hiện có
                ingredient.Id = _ingredients.Any() ? _ingredients.Max(x => x.Id) + 1 : 1;

                // Thêm vào danh sách static
                _ingredients.Add(ingredient);

                // Chuyển hướng về trang danh sách
                return RedirectToAction(nameof(Index));
            }
            return View(ingredient);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var item = _ingredients.FirstOrDefault(x => x.Id == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        public IActionResult Edit(Ingredient ingredient)
        {
            if (ModelState.IsValid)
            {
                var existingItem = _ingredients.FirstOrDefault(x => x.Id == ingredient.Id);
                if (existingItem != null)
                {
                    existingItem.Name = ingredient.Name;
                    existingItem.Unit = ingredient.Unit;
                    existingItem.StockQuantity = ingredient.StockQuantity;
                    existingItem.LowStockThreshold = ingredient.LowStockThreshold;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(ingredient);
        }

        public IActionResult Delete(int id)
        {
            var item = _ingredients.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                _ingredients.Remove(item);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}