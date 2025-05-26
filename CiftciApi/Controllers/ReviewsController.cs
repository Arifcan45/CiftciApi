using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CiftciApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace CiftciApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Reviews/ForUser/5
        [HttpGet("ForUser/{userId}")]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviewsForUser(int userId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.Reviewer)
                .Where(r => r.ReviewedUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(r => new ReviewDto
            {
                Id = r.Id,
                ReviewerId = r.ReviewerId,
                ReviewerName = r.Reviewer.Name,
                ReviewerImage = r.Reviewer.ProfileImageUrl,
                ReviewedUserId = r.ReviewedUserId,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        // POST: api/Reviews
        [HttpPost]
        public async Task<ActionResult<Review>> CreateReview(CreateReviewModel model)
        {
            var reviewer = await _context.Users.FindAsync(model.ReviewerId);
            if (reviewer == null)
            {
                return BadRequest("Değerlendiren kullanıcı bulunamadı.");
            }

            var reviewedUser = await _context.Users.FindAsync(model.ReviewedUserId);
            if (reviewedUser == null)
            {
                return BadRequest("Değerlendirilen kullanıcı bulunamadı.");
            }

            // Kullanıcının daha önce bu çiftçiyi değerlendirip değerlendirmediğini kontrol et
            if (await _context.Reviews.AnyAsync(r => r.ReviewerId == model.ReviewerId && r.ReviewedUserId == model.ReviewedUserId))
            {
                return BadRequest("Bu kullanıcıyı daha önce değerlendirdiniz.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var review = new Review
                {
                    ReviewerId = model.ReviewerId,
                    ReviewedUserId = model.ReviewedUserId,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    CreatedAt = DateTime.Now
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // Kullanıcının ortalama puanını ve yorum sayısını güncelle
                var reviews = await _context.Reviews
                    .Where(r => r.ReviewedUserId == model.ReviewedUserId)
                    .ToListAsync();

                reviewedUser.Rating = reviews.Average(r => r.Rating);
                reviewedUser.ReviewCount = reviews.Count;

                await _context.SaveChangesAsync();

                // Bildirim oluştur
                var notification = new Notification
                {
                    UserId = model.ReviewedUserId,
                    Title = "Yeni Değerlendirme",
                    Content = $"{reviewer.Name} size {model.Rating} yıldız verdi: {model.Comment}",
                    Type = NotificationType.NewReview,
                    IsRead = false,
                    RelatedEntityId = model.ReviewerId,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetReviewsForUser), new { userId = model.ReviewedUserId }, review);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // GET: api/Reviews/CanReview/1/2
        [HttpGet("CanReview/{reviewerId}/{reviewedUserId}")]
        public async Task<ActionResult<bool>> CanUserReview(int reviewerId, int reviewedUserId)
        {
            var hasReviewed = await _context.Reviews.AnyAsync(r =>
                r.ReviewerId == reviewerId && r.ReviewedUserId == reviewedUserId);

            return !hasReviewed;
        }
    }

    public class ReviewDto
    {
        public int Id { get; set; }
        public int ReviewerId { get; set; }
        public string ReviewerName { get; set; }
        public string ReviewerImage { get; set; }
        public int ReviewedUserId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateReviewModel
    {
        [Required]
        public int ReviewerId { get; set; }

        [Required]
        public int ReviewedUserId { get; set; }

        [Required, Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(500)]
        public string Comment { get; set; }
    }
}