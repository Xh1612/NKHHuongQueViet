using HuongQueViet.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuongQueViet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CouponController : ControllerBase
    {
        private readonly AppDbContext _context; // Đã đổi tên ở đây

        public CouponController(AppDbContext context) // Đã đổi tên ở đây
        {
            _context = context;
        }

        [HttpGet("check")]
        public async Task<IActionResult> CheckCoupon(string code, decimal orderValue)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Ok(new { success = false, message = "Vui lòng nhập mã giảm giá!" });
            }

            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == code && c.IsActive && c.ExpiryDate >= DateTime.Now);

            if (coupon == null)
            {
                return Ok(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã hết hạn!" });
            }

            if (orderValue < coupon.MinOrderValue)
            {
                return Ok(new
                {
                    success = false,
                    message = $"Đơn hàng tối thiểu phải từ {coupon.MinOrderValue:N0} đ để dùng mã này!"
                });
            }

            decimal discountAmount = 0;
            if (coupon.DiscountType == 0)
            {
                discountAmount = (orderValue * coupon.DiscountValue) / 100;
            }
            else
            {
                discountAmount = coupon.DiscountValue;
            }

            return Ok(new
            {
                success = true,
                discountAmount = discountAmount,
                message = "Áp dụng mã giảm giá thành công!"
            });
        }
    }
}