using Microsoft.AspNetCore.Mvc;
using HuongQueViet.Data;
using HuongQueViet.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;

namespace HuongQueViet.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;

        public PaymentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            return RedirectToAction("FakeVnPay", new { orderId = order.Id });
        }

        [HttpGet]
        public async Task<IActionResult> FakeVnPay(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessFakePayment(int orderId, bool isSuccess)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return RedirectToAction("Failed");

            if (isSuccess)
            {
                order.IsPaid = true;
                order.TransactionId = "VNPAY_SIM_" + DateTime.Now.ToString("yyyyMMddHHmmss");

                // Trừ kho khi thanh toán thành công qua VNPay
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= item.Quantity;
                        if (product.StockQuantity < 0) product.StockQuantity = 0;
                    }
                }

                await _context.SaveChangesAsync();

                return RedirectToAction("Success", new { orderId = order.Id });
            }
            else
            {
                order.Status = OrderStatus.Cancelled;
                await _context.SaveChangesAsync();

                return RedirectToAction("Failed", new { orderId = order.Id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return RedirectToAction("Index", "Home");

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> Failed(int? orderId)
        {
            if (orderId.HasValue)
            {
                var order = await _context.Orders.FindAsync(orderId.Value);
                ViewBag.Order = order;
            }
            return View();
        }
    }
}