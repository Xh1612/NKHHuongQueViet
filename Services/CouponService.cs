using Microsoft.EntityFrameworkCore;
using HuongQueViet.Data;

namespace HuongQueViet.Services
{
    public interface ICouponService { Task<(bool IsValid, string Message, decimal DiscountAmount)> ValidateAndCalculate(string code, decimal orderSubTotal); }

    public class CouponService : ICouponService
    {
        private readonly AppDbContext _context;
        public CouponService(AppDbContext context) { _context = context; }

        public async Task<(bool, string, decimal)> ValidateAndCalculate(string code, decimal orderSubTotal)
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == code && c.IsActive);
            if (coupon == null) return (false, "Mã giảm giá không tồn tại", 0);
            if (coupon.ExpiryDate < DateTime.Now) return (false, "Mã giảm giá đã hết hạn", 0);
            if (orderSubTotal < coupon.MinOrderValue) return (false, $"Đơn hàng cần tối thiểu {coupon.MinOrderValue:N0} đ", 0);
            var discount = coupon.DiscountType == Models.DiscountType.Percentage ? orderSubTotal * coupon.DiscountValue / 100 : coupon.DiscountValue;
            return (true, "OK", Math.Round(discount, 0));
        }
    }
}