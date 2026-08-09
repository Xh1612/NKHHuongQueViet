using System.Net;

namespace HuongQueViet.Models
{
    public enum OrderStatus { Pending, Confirmed, Preparing, Delivering, Completed, Cancelled }

    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int AddressId { get; set; }
        public Address? Address { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal ShippingFee { get; set; }
        public DateTime? ETA { get; set; }
        public string PaymentMethod { get; set; } = "COD";
        public bool IsPaid { get; set; }
        public string? TransactionId { get; set; }
        public string? CouponCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new();

        // Bổ sung cho Shipper (Giai đoạn 2)
        public string? ProofImageUrl { get; set; } // Ảnh xác nhận giao hàng thành công
        public string? FailureReason { get; set; } // Lý do nếu giao thất bại
    }
}