namespace HuongQueViet.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSpicy { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}