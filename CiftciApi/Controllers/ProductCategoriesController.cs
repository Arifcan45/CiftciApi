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
    public class ProductCategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductCategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ProductCategories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductCategory>>> GetProductCategories()
        {
            return await _context.ProductCategories
                .Include(pc => pc.SubCategories)
                .ToListAsync();
        }

        // GET: api/ProductCategories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductCategory>> GetProductCategory(int id)
        {
            var productCategory = await _context.ProductCategories
                .Include(pc => pc.SubCategories)
                .FirstOrDefaultAsync(pc => pc.Id == id);

            if (productCategory == null)
            {
                return NotFound();
            }

            return productCategory;
        }

        // POST: api/ProductCategories
        [HttpPost]
        public async Task<ActionResult<ProductCategory>> CreateProductCategory(ProductCategory productCategory)
        {
            _context.ProductCategories.Add(productCategory);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProductCategory", new { id = productCategory.Id }, productCategory);
        }

        // PUT: api/ProductCategories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProductCategory(int id, ProductCategory productCategory)
        {
            if (id != productCategory.Id)
            {
                return BadRequest();
            }

            _context.Entry(productCategory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductCategoryExists(id))
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

        // DELETE: api/ProductCategories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProductCategory(int id)
        {
            var productCategory = await _context.ProductCategories
                .Include(pc => pc.SubCategories)
                .FirstOrDefaultAsync(pc => pc.Id == id);

            if (productCategory == null)
            {
                return NotFound();
            }

            // Alt kategorileri sil
            _context.ProductSubCategories.RemoveRange(productCategory.SubCategories);

            // Bu kategoriye ait ürünleri kontrol et
            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                return BadRequest("Bu kategoriye ait ürünler olduğu için kategori silinemez.");
            }

            _context.ProductCategories.Remove(productCategory);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductCategoryExists(int id)
        {
            return _context.ProductCategories.Any(pc => pc.Id == id);
        }
    }
}