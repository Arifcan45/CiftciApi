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
    public class ProductSubCategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductSubCategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ProductSubCategories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductSubCategory>>> GetProductSubCategories()
        {
            return await _context.ProductSubCategories
                .Include(psc => psc.Category)
                .ToListAsync();
        }

        // GET: api/ProductSubCategories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductSubCategory>> GetProductSubCategory(int id)
        {
            var productSubCategory = await _context.ProductSubCategories
                .Include(psc => psc.Category)
                .FirstOrDefaultAsync(psc => psc.Id == id);

            if (productSubCategory == null)
            {
                return NotFound();
            }

            return productSubCategory;
        }

        // GET: api/ProductSubCategories/ByCategory/5
        [HttpGet("ByCategory/{categoryId}")]
        public async Task<ActionResult<IEnumerable<ProductSubCategory>>> GetProductSubCategoriesByCategoryId(int categoryId)
        {
            return await _context.ProductSubCategories
                .Where(psc => psc.CategoryId == categoryId)
                .ToListAsync();
        }

        // POST: api/ProductSubCategories
        [HttpPost]
        public async Task<ActionResult<ProductSubCategory>> CreateProductSubCategory(ProductSubCategory productSubCategory)
        {
            // Kategori kontrolü
            var category = await _context.ProductCategories.FindAsync(productSubCategory.CategoryId);
            if (category == null)
            {
                return BadRequest("Belirtilen kategori bulunamadı.");
            }

            _context.ProductSubCategories.Add(productSubCategory);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProductSubCategory", new { id = productSubCategory.Id }, productSubCategory);
        }

        // PUT: api/ProductSubCategories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProductSubCategory(int id, ProductSubCategory productSubCategory)
        {
            if (id != productSubCategory.Id)
            {
                return BadRequest();
            }

            // Kategori kontrolü
            var category = await _context.ProductCategories.FindAsync(productSubCategory.CategoryId);
            if (category == null)
            {
                return BadRequest("Belirtilen kategori bulunamadı.");
            }

            _context.Entry(productSubCategory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductSubCategoryExists(id))
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

        // DELETE: api/ProductSubCategories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProductSubCategory(int id)
        {
            var productSubCategory = await _context.ProductSubCategories.FindAsync(id);
            if (productSubCategory == null)
            {
                return NotFound();
            }

            // Bu alt kategoriye ait ürünleri kontrol et
            var hasProducts = await _context.Products.AnyAsync(p => p.SubCategoryId == id);
            if (hasProducts)
            {
                return BadRequest("Bu alt kategoriye ait ürünler olduğu için alt kategori silinemez.");
            }

            _context.ProductSubCategories.Remove(productSubCategory);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductSubCategoryExists(int id)
        {
            return _context.ProductSubCategories.Any(psc => psc.Id == id);
        }
    }
}