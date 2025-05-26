using Microsoft.EntityFrameworkCore;

namespace CiftciApi.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<ProductSubCategory> ProductSubCategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Media> Media { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Enum dönüşümlerini yapılandırma
            modelBuilder.Entity<User>()
                .Property(u => u.UserType)
                .HasConversion<int>();

            modelBuilder.Entity<Product>()
                .Property(p => p.Status)
                .HasConversion<int>();

            modelBuilder.Entity<Media>()
                .Property(m => m.Type)
                .HasConversion<int>();

            modelBuilder.Entity<Notification>()
                .Property(n => n.Type)
                .HasConversion<int>();

            // İlişkileri yapılandırma
            modelBuilder.Entity<User>()
                .HasOne(u => u.Location)
                .WithMany(l => l.Users)
                .HasForeignKey(u => u.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.User)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.SubCategory)
                .WithMany(sc => sc.Products)
                .HasForeignKey(p => p.SubCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Location)
                .WithMany(l => l.Products)
                .HasForeignKey(p => p.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductSubCategory>()
                .HasOne(psc => psc.Category)
                .WithMany(pc => pc.SubCategories)
                .HasForeignKey(psc => psc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Media>()
                .HasOne(m => m.Product)
                .WithMany(p => p.Media)
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Reviewer)
                .WithMany(u => u.ReviewsGiven)
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.ReviewedUser)
                .WithMany(u => u.ReviewsReceived)
                .HasForeignKey(r => r.ReviewedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.MessagesSent)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.MessagesReceived)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed Data (Temel kategoriler)
            modelBuilder.Entity<ProductCategory>().HasData(
                new ProductCategory { Id = 1, Name = "Sebze", IconUrl = "/icons/vegetables.png" },
                new ProductCategory { Id = 2, Name = "Meyve", IconUrl = "/icons/fruits.png" },
                new ProductCategory { Id = 3, Name = "Tahıl", IconUrl = "/icons/grains.png" },
                new ProductCategory { Id = 4, Name = "Baklagil", IconUrl = "/icons/legumes.png" },
                new ProductCategory { Id = 5, Name = "Süt Ürünleri", IconUrl = "/icons/dairy.png" }
            );

            modelBuilder.Entity<ProductSubCategory>().HasData(
                new ProductSubCategory { Id = 1, Name = "Domates", CategoryId = 1 },
                new ProductSubCategory { Id = 2, Name = "Pembe Domates", CategoryId = 1 },
                new ProductSubCategory { Id = 3, Name = "Salatalık", CategoryId = 1 },
                new ProductSubCategory { Id = 4, Name = "Elma", CategoryId = 2 },
                new ProductSubCategory { Id = 5, Name = "Buğday", CategoryId = 3 },
                new ProductSubCategory { Id = 6, Name = "Kuru Fasulye", CategoryId = 4 },
                new ProductSubCategory { Id = 7, Name = "Peynir", CategoryId = 5 }
            );
        }
    }
}