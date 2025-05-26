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
        public DbSet<Product> Products { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Çiftçi ve alıcı tiplerini ayarla
            modelBuilder.Entity<User>()
                .Property(u => u.UserType)
                .HasConversion<int>();

            // Ürün satılabilir durumda olsun
            modelBuilder.Entity<Product>()
                .Property(p => p.IsAvailable)
                .HasDefaultValue(true);

            // Kullanıcının ortalama puanı ve değerlendirme sayısı
            modelBuilder.Entity<User>()
                .Property(u => u.Rating)
                .HasDefaultValue(0);

            modelBuilder.Entity<User>()
                .Property(u => u.ReviewCount)
                .HasDefaultValue(0);

            // Foreign key ilişkileri
            modelBuilder.Entity<Product>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.ReviewedUser)
                .WithMany()
                .HasForeignKey(r => r.ReviewedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}