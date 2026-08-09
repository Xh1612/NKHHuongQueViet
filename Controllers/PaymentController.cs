using Microsoft.AspNetCore.Mvc;
using HuongQueViet.Data;
using HuongQueViet.Models;
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

        // 1. Khi nhấn "Đặt hàng ngay" với phương thức VNPay
        [HttpGet]
        public async Task<IActionResult> Create(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            // Điều hướng sang trang giả lập VNPay
            return RedirectToAction("FakeVnPay", new { orderId = order.Id });
        }

        // 2. Hiển thị giao diện giả lập Cổng thanh toán VNPay
        [HttpGet]
        public async Task<IActionResult> FakeVnPay(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            return View(order);
        }

        // 3. Xử lý khi bấm nút "Thanh toán thành công" hoặc "Hủy thanh toán"
        [HttpPost]
        public async Task<IActionResult> ProcessFakePayment(int orderId, bool isSuccess)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return RedirectToAction("Failed");

            if (isSuccess)
            {
                // Cập nhật trạng thái đã thanh toán
                order.IsPaid = true;
                order.TransactionId = "VNPAY_SIM_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                await _context.SaveChangesAsync();

                return RedirectToAction("Success", new { orderId = order.Id });
            }

            return RedirectToAction("Failed", new { orderId = order.Id });
        }

        // 4. Trang thông báo kết quả
        public IActionResult Success(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }

        public IActionResult Failed(int? orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }
    }
}