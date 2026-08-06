namespace HuongQueViet.Models
{
    public class StatisticsViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public List<StatusCount> OrdersByStatus { get; set; } = new();
        public List<DailyRevenue> RevenueByDay { get; set; } = new();
        public List<ProductSales> TopProducts { get; set; } = new();
    }
    public class StatusCount { public string Status { get; set; } = string.Empty; public int Count { get; set; } }
    public class DailyRevenue { public DateTime Date { get; set; } public decimal Revenue { get; set; } }
    public class ProductSales { public string ProductName { get; set; } = string.Empty; public int QuantitySold { get; set; } }
}