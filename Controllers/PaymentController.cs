using Microsoft.AspNetCore.Mvc;
using HuongQueViet.Data;
using HuongQueViet.Services;

namespace HuongQueViet.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IVnPayService _vnPayService;
        public PaymentController(AppDbContext context, IVnPayService vnPayService) { _context = context; _vnPayService = vnPayService; }

        public async Task<IActionResult> Create(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();
            return Redirect(_vnPayService.CreatePaymentUrl(order, HttpContext));
        }

        [HttpGet]
        public async Task<IActionResult> Callback()
        {
            var (isValid, isSuccess, txnRef) = _vnPayService.ProcessReturn(Request.Query);
            if (!isValid || string.IsNullOrEmpty(txnRef)) return RedirectToAction("Failed");
            var order = await _context.Orders.FindAsync(int.Parse(txnRef.Split('_')[0]));
            if (order == null) return RedirectToAction("Failed");
            if (isSuccess)
            {
                order.IsPaid = true; order.TransactionId = txnRef;
                await _context.SaveChangesAsync();
                return RedirectToAction("Success", new { orderId = order.Id });
            }
            return RedirectToAction("Failed", new { orderId = order.Id });
        }

        public IActionResult Success(int orderId) { ViewBag.OrderId = orderId; return View(); }
        public IActionResult Failed(int? orderId) { ViewBag.OrderId = orderId; return View(); }
    }
}