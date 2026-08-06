namespace HuongQueViet.Models
{
    public class DeliveryZone
    {
        public int Id { get; set; }
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public decimal BaseFee { get; set; }
        public decimal FeePerKm { get; set; }
    }
}