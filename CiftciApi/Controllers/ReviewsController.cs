using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CiftciApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        // GET: api/Reviews
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Review>>> GetReviews()
        {
            return await _context.Reviews
                .Include(r => r.Reviewer)
                .Include(r => r.ReviewedUser)
                .ToListAsync();
        }

        // GET: api/Reviews/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Review>> GetReview(int id)
        {
            var review = await _context.Reviews
                .Include(r => r.Reviewer)
                .Include(r => r.ReviewedUser)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
            {
                return NotFound();
            }

            return review;
        }

        // POST: api/Reviews
        [HttpPost]
        public async Task<ActionResult<Review>> PostReview(Review review)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // Kullanıcının ortalama puanını ve yorum sayısını güncelle
                var user = await _context.Users.FindAsync(review.ReviewedUserId);
                if (user != null)
                {
                    var reviews = await _context.Reviews
                        .Where(r => r.ReviewedUserId == user.Id)
                        .ToListAsync();

                    user.Rating = reviews.Average(r => r.Rating);
                    user.ReviewCount = reviews.Count;

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return CreatedAtAction("GetReview", new { id = review.Id }, review);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // GET: api/Reviews/ForUser/5
        [HttpGet("ForUser/{userId}")]
        public async Task<ActionResult<IEnumerable<Review>>> GetReviewsForUser(int userId)
        {
            return await _context.Reviews
                .Include(r => r.Reviewer)
                .Where(r => r.ReviewedUserId == userId)
                .ToListAsync();
        }

        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(e => e.Id == id);
        }
    }
}