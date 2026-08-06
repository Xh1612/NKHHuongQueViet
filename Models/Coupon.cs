namespace HuongQueViet.Models
{
    public enum DiscountType { Percentage, FixedAmount }

    public class Coupon
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal MinOrderValue { get; set; }
        public bool IsActive { get; set; } = true;
    }

}
