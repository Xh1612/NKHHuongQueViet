namespace HuongQueViet.Models
{
    public class ShipperOrderViewModel
    {
        public int Id { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ETA { get; set; }

        // Khách hàng
        public string ReceiverName { get; set; } = "";
        public string ReceiverPhone { get; set; } = "";

        // Địa chỉ
        public string Street { get; set; } = "";
        public string Ward { get; set; } = "";
        public string District { get; set; } = "";
        public string Province { get; set; } = "";
        public double Lat { get; set; }
        public double Lng { get; set; }

        // Tiền
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public string PaymentMethod { get; set; } = "";
        public bool IsPaid { get; set; }
        public string? CouponCode { get; set; }

        // Bổ sung thuộc tính lưu lý do giao thất bại (Sửa lỗi CS0117)
        public string? FailureReason { get; set; }

        public List<ShipperOrderItemViewModel> Items { get; set; } = new();
    }

    public class ShipperOrderItemViewModel
    {
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }
}