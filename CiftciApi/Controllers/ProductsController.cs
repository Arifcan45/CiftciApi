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
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products
                .Include(p => p.User)
                .Where(p => p.IsAvailable)
                .ToListAsync();
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProduct", new { id = product.Id }, product);
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            product.UpdatedAt = DateTime.Now;
            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Products/Filter
        [HttpGet("Filter")]
        public async Task<ActionResult<IEnumerable<Product>>> FilterProducts(
            [FromQuery] string category = null,
            [FromQuery] string subCategory = null,
            [FromQuery] double? minPrice = null,
            [FromQuery] double? maxPrice = null,
            [FromQuery] double? minQuantity = null,
            [FromQuery] double? latitude = null,
            [FromQuery] double? longitude = null,
            [FromQuery] int? radius = null,
            [FromQuery] bool onlyAvailable = true,
            [FromQuery] double? minRating = null)
        {
            var query = _context.Products
                .Include(p => p.User)
                .Where(p => !onlyAvailable || p.IsAvailable);

            // Kategori filtreleme
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category.ToLower() == category.ToLower());
            }

            // Alt kategori filtreleme
            if (!string.IsNullOrEmpty(subCategory))
            {
                query = query.Where(p => p.SubCategory.ToLower() == subCategory.ToLower());
            }

            // Fiyat filtreleme
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.PricePerUnit >= (decimal)minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.PricePerUnit <= (decimal)maxPrice.Value);
            }

            // Miktar filtreleme
            if (minQuantity.HasValue)
            {
                query = query.Where(p => p.Quantity >= minQuantity.Value);
            }

            // Konum bazlı filtreleme
            if (latitude.HasValue && longitude.HasValue && radius.HasValue)
            {
                // Haversine formülü SQL Server'da karmaşık olduğundan,
                // Burada daha basit bir yaklaşım kullanıyoruz:
                // Belli bir enlem ve boylam aralığındaki ürünleri getir
                double latDiff = radius.Value / 111.12; // km başına yaklaşık derece farkı
                double lonDiff = radius.Value / (111.12 * Math.Cos(latitude.Value * Math.PI / 180));

                query = query.Where(p =>
                    p.Latitude >= latitude.Value - latDiff &&
                    p.Latitude <= latitude.Value + latDiff &&
                    p.Longitude >= longitude.Value - lonDiff &&
                    p.Longitude <= longitude.Value + lonDiff);
            }

            // Çiftçi puanı filtreleme
            if (minRating.HasValue)
            {
                query = query.Where(p => p.User.Rating >= minRating.Value);
            }

            return await query.ToListAsync();
        }

        // GET: api/Products/ByUser/5
        [HttpGet("ByUser/{userId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByUser(int userId)
        {
            return await _context.Products
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}