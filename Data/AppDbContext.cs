using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HuongQueViet.Models;

namespace HuongQueViet.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<DeliveryZone> DeliveryZones { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<ProductIngredient> ProductIngredients { get; set; }
        public DbSet<InventoryLog> InventoryLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Phở & Bún" },
                new Category { Id = 2, Name = "Cơm" },
                new Category { Id = 3, Name = "Ăn vặt & Cuốn" }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Phở bò tái", Description = "Nước dùng hầm xương 8 tiếng", Price = 55000, StockQuantity = 30, CategoryId = 1, IsFeatured = true, CreatedAt = new DateTime(2026, 1, 1) },
                new Product { Id = 2, Name = "Bún chả Hà Nội", Description = "Chả nướng than hoa", Price = 50000, StockQuantity = 25, CategoryId = 1, IsFeatured = true, CreatedAt = new DateTime(2026, 1, 2) },
                new Product { Id = 3, Name = "Cơm tấm sườn bì", Description = "Sườn nướng mật ong, bì, chả trứng", Price = 45000, StockQuantity = 20, CategoryId = 2, CreatedAt = new DateTime(2026, 1, 3) },
                new Product { Id = 4, Name = "Gỏi cuốn tôm thịt", Description = "Cuốn tay mỗi phần", Price = 35000, StockQuantity = 40, CategoryId = 3, CreatedAt = new DateTime(2026, 1, 4) }
            );

            modelBuilder.Entity<DeliveryZone>().HasData(
                new DeliveryZone { Id = 1, Province = "TP.HCM", District = "Quận 1", BaseFee = 15000, FeePerKm = 4000 },
                new DeliveryZone { Id = 2, Province = "TP.HCM", District = "Quận 3", BaseFee = 15000, FeePerKm = 4000 },
                new DeliveryZone { Id = 3, Province = "TP.HCM", District = "Thủ Đức", BaseFee = 20000, FeePerKm = 5000 }
            );
        }
    }
}