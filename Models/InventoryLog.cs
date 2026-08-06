namespace HuongQueViet.Models
{
    public class InventoryLog
    {
        public int Id { get; set; }
        public int IngredientId { get; set; }
        public decimal Change { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

