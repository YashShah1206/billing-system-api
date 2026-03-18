using BillingSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Shop> Shops => Set<Shop>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
        public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
        public DbSet<Bill> Bills => Set<Bill>();
        public DbSet<BillItem> BillItems => Set<BillItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Email).IsUnique();
                e.Property(x => x.Email).HasMaxLength(200).IsRequired();
                e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
                e.Property(x => x.PhoneNumber).HasMaxLength(15);
                e.Property(x => x.PasswordHash).IsRequired();
                e.HasQueryFilter(x => !x.IsDeleted);

                e.HasOne(x => x.ApprovedByAdmin)
                 .WithMany()
                 .HasForeignKey(x => x.ApprovedByAdminId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // Shop
            modelBuilder.Entity<Shop>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.ShopName).HasMaxLength(200).IsRequired();
                e.Property(x => x.GstNumber).HasMaxLength(20);
                e.Property(x => x.PanNumber).HasMaxLength(15);
            });

            // Category
            modelBuilder.Entity<Category>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Name).IsUnique();
                e.Property(x => x.Name).HasMaxLength(100).IsRequired();
                e.HasQueryFilter(x => !x.IsDeleted);
            });

            // Supplier
            modelBuilder.Entity<Supplier>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.SupplierName).HasMaxLength(200).IsRequired();
                e.Property(x => x.GstNumber).HasMaxLength(20);
                e.HasQueryFilter(x => !x.IsDeleted);
            });

            // Product
            modelBuilder.Entity<Product>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.ProductCode).IsUnique();
                e.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
                e.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
                e.Property(x => x.BuyingPrice).HasPrecision(18, 4);
                e.Property(x => x.SellingPrice).HasPrecision(18, 4);
                e.Property(x => x.MRP).HasPrecision(18, 4);
                e.Property(x => x.GstRate).HasPrecision(5, 2);
                e.Property(x => x.CessRate).HasPrecision(5, 2);
                e.HasQueryFilter(x => !x.IsDeleted);

                e.HasOne(x => x.Category)
                 .WithMany(x => x.Products)
                 .HasForeignKey(x => x.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Supplier)
                 .WithMany(x => x.Products)
                 .HasForeignKey(x => x.SupplierId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // PurchaseOrder
            modelBuilder.Entity<PurchaseOrder>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.PurchaseBillNumber).HasMaxLength(50).IsRequired();
                e.Property(x => x.SubTotal).HasPrecision(18, 4);
                e.Property(x => x.TotalGst).HasPrecision(18, 4);
                e.Property(x => x.TotalCess).HasPrecision(18, 4);
                e.Property(x => x.TotalAmount).HasPrecision(18, 4);
                e.HasQueryFilter(x => !x.IsDeleted);

                e.HasOne(x => x.Supplier)
                 .WithMany(x => x.PurchaseOrders)
                 .HasForeignKey(x => x.SupplierId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // PurchaseOrderItem
            modelBuilder.Entity<PurchaseOrderItem>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.BuyingPrice).HasPrecision(18, 4);
                e.Property(x => x.GstAmount).HasPrecision(18, 4);
                e.Property(x => x.TotalAmount).HasPrecision(18, 4);
            });

            // Bill
            modelBuilder.Entity<Bill>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.BillNumber).IsUnique();
                e.Property(x => x.BillNumber).HasMaxLength(50).IsRequired();
                e.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
                e.Property(x => x.SubTotal).HasPrecision(18, 4);
                e.Property(x => x.DiscountAmount).HasPrecision(18, 4);
                e.Property(x => x.TaxableAmount).HasPrecision(18, 4);
                e.Property(x => x.CgstAmount).HasPrecision(18, 4);
                e.Property(x => x.SgstAmount).HasPrecision(18, 4);
                e.Property(x => x.IgstAmount).HasPrecision(18, 4);
                e.Property(x => x.CessAmount).HasPrecision(18, 4);
                e.Property(x => x.TotalAmount).HasPrecision(18, 4);
                e.HasQueryFilter(x => !x.IsDeleted);

                e.HasOne(x => x.CreatedByUser)
                 .WithMany(x => x.Bills)
                 .HasForeignKey(x => x.CreatedByUserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // BillItem
            modelBuilder.Entity<BillItem>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.SellingPrice).HasPrecision(18, 4);
                e.Property(x => x.TaxableAmount).HasPrecision(18, 4);
                e.Property(x => x.CgstAmount).HasPrecision(18, 4);
                e.Property(x => x.SgstAmount).HasPrecision(18, 4);
                e.Property(x => x.IgstAmount).HasPrecision(18, 4);
                e.Property(x => x.CessAmount).HasPrecision(18, 4);
                e.Property(x => x.TotalAmount).HasPrecision(18, 4);
            });

            // Seed default shop
            modelBuilder.Entity<Shop>().HasData(new Shop
            {
                Id = 1,
                ShopName = "My Shop",
                OwnerName = "Shop Owner",
                Address = "Shop Address",
                City = "City",
                State = "Gujarat",
                PinCode = "380001",
                PhoneNumber = "9999999999",
                Email = "shop@example.com",
                GstNumber = "24AAAAA0000A1Z5",
                PanNumber = "AAAAA0000A",
                TermsAndConditions = "Goods once sold will not be taken back.\nAll disputes subject to local jurisdiction.",
                CreatedAt = new DateTime(2024, 1, 1)
            });

            // Seed default admin
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                FullName = "System Admin",
                Email = "admin@billing.com",
                PhoneNumber = "9999999999",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = Core.Enums.UserRole.Admin,
                Status = Core.Enums.UserStatus.Approved,
                ApprovedAt = new DateTime(2024, 1, 1),
                CreatedAt = new DateTime(2024, 1, 1)
            });

            // Seed default category
            modelBuilder.Entity<Category>().HasData(new Category
            {
                Id = 1,
                Name = "General",
                Description = "General Products",
                CreatedAt = new DateTime(2024, 1, 1)
            });
        }
    }
}
