using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CiftciApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CiftciApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Dashboard/Stats
        [HttpGet("Stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
        {
            // Temel istatistikler
            var totalUsers = await _context.Users.CountAsync();
            var totalCiftciler = await _context.Users.CountAsync(u => u.UserType == UserType.Ciftci);
            var totalAlicilar = await _context.Users.CountAsync(u => u.UserType == UserType.Alici);
            var totalProducts = await _context.Products.CountAsync();
            var activeProducts = await _context.Products.CountAsync(p => p.Status == ProductStatus.Available);

            // En popüler kategoriler
            var popularCategories = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Category != null)
                .GroupBy(p => p.Category.Name)
                .Select(g => new CategoryStatDto
                {
                    Name = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(c => c.Count)
                .Take(5)
                .ToListAsync();

            // En son eklenen ürünler
            var latestProducts = await _context.Products
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .Include(p => p.Location)
                .Include(p => p.Media.Where(m => m.IsMain))
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Category = p.Category != null ? p.Category.Name : null,
                    SubCategory = p.SubCategory != null ? p.SubCategory.Name : null,
                    UserId = p.UserId,
                    UserName = p.User.Name,
                    UserType = p.User.UserType,
                    UserRating = p.User.Rating,
                    Province = p.Location != null ? p.Location.Province : null,
                    District = p.Location != null ? p.Location.District : null,
                    Village = p.Location != null ? p.Location.Village : null,
                    Quantity = p.Quantity,
                    Unit = p.Unit,
                    PricePerUnit = p.PricePerUnit,
                    FieldSize = p.FieldSize,
                    Status = p.Status,
                    ImageUrl = p.Media.FirstOrDefault(m => m.IsMain) != null ? p.Media.First(m => m.IsMain).Url : null,
                    HasVideo = p.Media.Any(m => m.Type == MediaType.Video),
                    Description = p.Description,
                    HarvestDate = p.HarvestDate,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            // En yüksek puanlı çiftçiler
            var topRatedFarmers = await _context.Users
                .Where(u => u.UserType == UserType.Ciftci && u.Rating.HasValue && u.ReviewCount.HasValue && u.ReviewCount > 0)
                .OrderByDescending(u => u.Rating)
                .Take(5)
                .Select(u => new UserRatingDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Rating = u.Rating.Value,
                    ReviewCount = u.ReviewCount.Value,
                    ProfileImageUrl = u.ProfileImageUrl
                })
                .ToListAsync();

            // Son kayıt olan kullanıcılar
            var recentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(10)
                .Select(u => new UserBasicDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    UserType = u.UserType,
                    ProfileImageUrl = u.ProfileImageUrl,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return new DashboardStatsDto
            {
                TotalUsers = totalUsers,
                TotalCiftciler = totalCiftciler,
                TotalAlicilar = totalAlicilar,
                TotalProducts = totalProducts,
                ActiveProducts = activeProducts,
                PopularCategories = popularCategories,
                LatestProducts = latestProducts,
                TopRatedFarmers = topRatedFarmers,
                RecentUsers = recentUsers
            };
        }

        // GET: api/Dashboard/User/5
        [HttpGet("User/{id}")]
        public async Task<ActionResult<UserDashboardDto>> GetUserDashboard(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = new UserDashboardDto
            {
                UserId = id,
                UserType = user.UserType
            };

            // Çiftçi için özel istatistikler
            if (user.UserType == UserType.Ciftci)
            {
                result.TotalProducts = await _context.Products.CountAsync(p => p.UserId == id);
                result.ActiveProducts = await _context.Products.CountAsync(p => p.UserId == id && p.Status == ProductStatus.Available);
                result.SoldProducts = await _context.Products.CountAsync(p => p.UserId == id && p.Status == ProductStatus.Sold);
                result.AverageRating = user.Rating ?? 0;
                result.ReviewCount = user.ReviewCount ?? 0;

                // Son mesajlar
                result.RecentMessages = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .Where(m => m.ReceiverId == id)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(5)
                    .Select(m => new RecentMessageDto
                    {
                        Id = m.Id,
                        SenderId = m.SenderId,
                        SenderName = m.Sender.Name,
                        Content = m.Content.Length > 50 ? m.Content.Substring(0, 50) + "..." : m.Content,
                        IsRead = m.IsRead,
                        CreatedAt = m.CreatedAt
                    })
                    .ToListAsync();

                // Son değerlendirmeler
                result.RecentReviews = await _context.Reviews
                    .Include(r => r.Reviewer)
                    .Where(r => r.ReviewedUserId == id)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .Select(r => new RecentReviewDto
                    {
                        Id = r.Id,
                        ReviewerId = r.ReviewerId,
                        ReviewerName = r.Reviewer.Name,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();

                // Ürün kategorileri dağılımı
                result.ProductCategoryDistribution = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.UserId == id && p.Category != null)
                    .GroupBy(p => p.Category.Name)
                    .Select(g => new CategoryStatDto
                    {
                        Name = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();
            }
            // Alıcı için özel istatistikler
            else
            {
                // Gönderilen mesaj sayısı
                result.SentMessageCount = await _context.Messages.CountAsync(m => m.SenderId == id);

                // Çiftçilerle iletişime geçme sayısı
                var uniqueFarmersContacted = await _context.Messages
                    .Include(m => m.Receiver)
                    .Where(m => m.SenderId == id && m.Receiver.UserType == UserType.Ciftci)
                    .Select(m => m.ReceiverId)
                    .Distinct()
                    .CountAsync();

                result.UniqueFarmersContacted = uniqueFarmersContacted;

                // Son mesajlaşılan çiftçiler
                result.RecentlyContactedFarmers = await _context.Messages
                    .Include(m => m.Receiver)
                    .Where(m => m.SenderId == id && m.Receiver.UserType == UserType.Ciftci)
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.ReceiverId)
                    .Distinct()
                    .Take(5)
                    .Select(farmerId => _context.Users.FirstOrDefault(u => u.Id == farmerId))
                    .Select(farmer => new UserBasicDto
                    {
                        Id = farmer.Id,
                        Name = farmer.Name,
                        UserType = farmer.UserType,
                        ProfileImageUrl = farmer.ProfileImageUrl,
                        Rating = farmer.Rating
                    })
                    .ToListAsync();
            }

            return result;
        }
    }

    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalCiftciler { get; set; }
        public int TotalAlicilar { get; set; }
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public List<CategoryStatDto> PopularCategories { get; set; }
        public List<ProductDto> LatestProducts { get; set; }
        public List<UserRatingDto> TopRatedFarmers { get; set; }
        public List<UserBasicDto> RecentUsers { get; set; }
    }

    public class CategoryStatDto
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class UserRatingDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public string ProfileImageUrl { get; set; }
    }

    public class UserBasicDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public UserType UserType { get; set; }
        public string ProfileImageUrl { get; set; }
        public double? Rating { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserDashboardDto
    {
        public int UserId { get; set; }
        public UserType UserType { get; set; }

        // Çiftçi için
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int SoldProducts { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public List<RecentMessageDto> RecentMessages { get; set; }
        public List<RecentReviewDto> RecentReviews { get; set; }
        public List<CategoryStatDto> ProductCategoryDistribution { get; set; }

        // Alıcı için
        public int SentMessageCount { get; set; }
        public int UniqueFarmersContacted { get; set; }
        public List<UserBasicDto> RecentlyContactedFarmers { get; set; }
    }

    public class RecentMessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RecentReviewDto
    {
        public int Id { get; set; }
        public int ReviewerId { get; set; }
        public string ReviewerName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}