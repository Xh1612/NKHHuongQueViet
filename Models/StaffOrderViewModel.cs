namespace HuongQueViet.Models
{
    public class StaffOrderViewModel
    {
        public int Id { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime OrderDate { get; set; }

        // Khách hàng
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";

        // Địa chỉ giao
        public string Street { get; set; } = "";
        public string Ward { get; set; } = "";
        public string District { get; set; } = "";
        public string Province { get; set; } = "";

        // Tiền
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public string PaymentMethod { get; set; } = "";
        public bool IsPaid { get; set; }
        public string? CouponCode { get; set; }

        public List<StaffOrderItemViewModel> Items { get; set; } = new();
    }

    public class StaffOrderItemViewModel
    {
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }
}