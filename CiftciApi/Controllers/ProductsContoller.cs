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
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            return await _context.Products
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .Include(p => p.Location)
                .Include(p => p.Media.Where(m => m.IsMain))
                .Where(p => p.Status == ProductStatus.Available)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Category = p.Category.Name,
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
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDetailDto>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .Include(p => p.Location)
                .Include(p => p.Media)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            var productDetail = new ProductDetailDto
            {
                Id = product.Id,
                Name = product.Name,
                Category = product.Category.Name,
                CategoryId = product.CategoryId,
                SubCategory = product.SubCategory != null ? product.SubCategory.Name : null,
                SubCategoryId = product.SubCategoryId,
                UserId = product.UserId,
                UserName = product.User.Name,
                UserType = product.User.UserType,
                UserRating = product.User.Rating,
                UserReviewCount = product.User.ReviewCount,
                UserProfileImage = product.User.ProfileImageUrl,
                Province = product.Location != null ? product.Location.Province : null,
                District = product.Location != null ? product.Location.District : null,
                Village = product.Location != null ? product.Location.Village : null,
                Latitude = product.Location != null ? product.Location.Latitude : 0,
                Longitude = product.Location != null ? product.Location.Longitude : 0,
                Quantity = product.Quantity,
                Unit = product.Unit,
                PricePerUnit = product.PricePerUnit,
                FieldSize = product.FieldSize,
                Status = product.Status,
                Description = product.Description,
                HarvestDate = product.HarvestDate,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                Media = product.Media.Select(m => new MediaDto
                {
                    Id = m.Id,
                    Url = m.Url,
                    Type = m.Type,
                    ThumbnailUrl = m.ThumbnailUrl,
                    IsMain = m.IsMain
                }).ToList()
            };

            return productDetail;
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(CreateProductModel model)
        {
            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null || user.UserType != UserType.Ciftci)
            {
                return BadRequest("Yalnızca çiftçiler ürün ekleyebilir.");
            }

            // Kategori ve alt kategori kontrolü
            var category = await _context.ProductCategories.FindAsync(model.CategoryId);
            if (category == null)
            {
                return BadRequest("Belirtilen kategori bulunamadı.");
            }

            ProductSubCategory subCategory = null;
            if (model.SubCategoryId.HasValue)
            {
                subCategory = await _context.ProductSubCategories.FindAsync(model.SubCategoryId.Value);
                if (subCategory == null)
                {
                    return BadRequest("Belirtilen alt kategori bulunamadı.");
                }

                if (subCategory.CategoryId != model.CategoryId)
                {
                    return BadRequest("Alt kategori, ana kategori ile eşleşmiyor.");
                }
            }

            // Lokasyon bilgileri
            Location location = null;
            if (model.LocationId.HasValue)
            {
                location = await _context.Locations.FindAsync(model.LocationId.Value);
                if (location == null)
                {
                    return BadRequest("Belirtilen lokasyon bulunamadı.");
                }
            }
            else if (model.Latitude.HasValue && model.Longitude.HasValue)
            {
                location = new Location
                {
                    Province = model.Province ?? user.Province ?? "Belirtilmedi",
                    District = model.District ?? user.District ?? "Belirtilmedi",
                    Village = model.Village ?? user.Village,
                    Latitude = model.Latitude.Value,
                    Longitude = model.Longitude.Value
                };

                _context.Locations.Add(location);
                await _context.SaveChangesAsync();
            }
            else if (user.LocationId.HasValue)
            {
                location = await _context.Locations.FindAsync(user.LocationId.Value);
            }

            var product = new Product
            {
                UserId = model.UserId,
                Name = model.Name,
                CategoryId = model.CategoryId,
                SubCategoryId = model.SubCategoryId,
                Quantity = model.Quantity,
                Unit = model.Unit,
                PricePerUnit = model.PricePerUnit,
                FieldSize = model.FieldSize,
                LocationId = location?.Id,
                Description = model.Description,
                HarvestDate = model.HarvestDate,
                Status = ProductStatus.Available,
                CreatedAt = DateTime.Now
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProduct", new { id = product.Id }, product);
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, UpdateProductModel model)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Ürünü güncelleyin
            if (!string.IsNullOrEmpty(model.Name))
                product.Name = model.Name;

            if (model.CategoryId.HasValue)
            {
                var category = await _context.ProductCategories.FindAsync(model.CategoryId.Value);
                if (category == null)
                {
                    return BadRequest("Belirtilen kategori bulunamadı.");
                }
                product.CategoryId = model.CategoryId.Value;
            }

            if (model.SubCategoryId.HasValue)
            {
                var subCategory = await _context.ProductSubCategories.FindAsync(model.SubCategoryId.Value);
                if (subCategory == null)
                {
                    return BadRequest("Belirtilen alt kategori bulunamadı.");
                }
                product.SubCategoryId = model.SubCategoryId.Value;
            }

            if (model.Quantity.HasValue)
                product.Quantity = model.Quantity.Value;

            if (!string.IsNullOrEmpty(model.Unit))
                product.Unit = model.Unit;

            if (model.PricePerUnit.HasValue)
                product.PricePerUnit = model.PricePerUnit.Value;

            if (!string.IsNullOrEmpty(model.FieldSize))
                product.FieldSize = model.FieldSize;

            if (!string.IsNullOrEmpty(model.Description))
                product.Description = model.Description;

            if (model.HarvestDate.HasValue)
                product.HarvestDate = model.HarvestDate;

            if (model.Status.HasValue)
                product.Status = model.Status.Value;

            product.UpdatedAt = DateTime.Now;

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
            var product = await _context.Products
                .Include(p => p.Media)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            _context.Media.RemoveRange(product.Media);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Products/Filter
        [HttpGet("Filter")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> FilterProducts([FromQuery] ProductFilterModel filter)
        {
            var query = _context.Products
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .Include(p => p.Location)
                .Include(p => p.Media)
                .AsQueryable();

            // Sadece uygun ürünleri göster
            if (filter.OnlyAvailable)
            {
                query = query.Where(p => p.Status == ProductStatus.Available);
            }

            // Kategori filtreleme
            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            // Alt kategori filtreleme
            if (filter.SubCategoryId.HasValue)
            {
                query = query.Where(p => p.SubCategoryId == filter.SubCategoryId.Value);
            }

            // İl bazlı filtreleme
            if (!string.IsNullOrEmpty(filter.Province))
            {
                query = query.Where(p => p.Location.Province == filter.Province);
            }

            // İlçe bazlı filtreleme
            if (!string.IsNullOrEmpty(filter.District))
            {
                query = query.Where(p => p.Location.District == filter.District);
            }

            // Köy bazlı filtreleme
            if (!string.IsNullOrEmpty(filter.Village))
            {
                query = query.Where(p => p.Location.Village == filter.Village);
            }

            // Fiyat filtreleme
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.PricePerUnit >= filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.PricePerUnit <= filter.MaxPrice.Value);
            }

            // Miktar filtreleme
            if (filter.MinQuantity.HasValue)
            {
                query = query.Where(p => p.Quantity >= filter.MinQuantity.Value);
            }

            // Hasat tarihi filtreleme
            if (filter.MinHarvestDate.HasValue)
            {
                query = query.Where(p => p.HarvestDate >= filter.MinHarvestDate.Value);
            }

            if (filter.MaxHarvestDate.HasValue)
            {
                query = query.Where(p => p.HarvestDate <= filter.MaxHarvestDate.Value);
            }

            // Konum bazlı filtreleme (yarıçap içinde)
            if (filter.Latitude.HasValue && filter.Longitude.HasValue && filter.Radius.HasValue)
            {
                double latDiff = filter.Radius.Value / 111.12;
                double lonDiff = filter.Radius.Value / (111.12 * Math.Cos(filter.Latitude.Value * Math.PI / 180));

                query = query.Where(p =>
                    p.Location.Latitude >= filter.Latitude.Value - latDiff &&
                    p.Location.Latitude <= filter.Latitude.Value + latDiff &&
                    p.Location.Longitude >= filter.Longitude.Value - lonDiff &&
                    p.Location.Longitude <= filter.Longitude.Value + lonDiff);
            }

            // Çiftçi puanı filtreleme
            if (filter.MinRating.HasValue)
            {
                query = query.Where(p => p.User.Rating >= filter.MinRating.Value);
            }

            // Video olanları filtreleme
            if (filter.HasVideo)
            {
                query = query.Where(p => p.Media.Any(m => m.Type == MediaType.Video));
            }

            // Sıralama
            switch (filter.SortBy)
            {
                case "price_asc":
                    query = query.OrderBy(p => p.PricePerUnit);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(p => p.PricePerUnit);
                    break;
                case "date_desc":
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
                case "rating_desc":
                    query = query.OrderByDescending(p => p.User.Rating);
                    break;
                case "quantity_desc":
                    query = query.OrderByDescending(p => p.Quantity);
                    break;
                default:
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            // Sayfalama
            if (filter.Page.HasValue && filter.PageSize.HasValue)
            {
                query = query.Skip((filter.Page.Value - 1) * filter.PageSize.Value)
                             .Take(filter.PageSize.Value);
            }

            var products = await query.ToListAsync();

            return products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Category = p.Category.Name,
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
            }).ToList();
        }

        // GET: api/Products/ByUser/5
        [HttpGet("ByUser/{userId}")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsByUser(int userId)
        {
            var products = await _context.Products
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .Include(p => p.Location)
                .Include(p => p.Media)
                .Where(p => p.UserId == userId)
                .ToListAsync();

            return products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Category = p.Category.Name,
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
            }).ToList();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public UserType UserType { get; set; }
        public double? UserRating { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Village { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; }
        public decimal PricePerUnit { get; set; }
        public string FieldSize { get; set; }
        public ProductStatus Status { get; set; }
        public string ImageUrl { get; set; }
        public bool HasVideo { get; set; }
        public string Description { get; set; }
        public DateTime? HarvestDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ProductDetailDto : ProductDto
    {
        public int CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public int? UserReviewCount { get; set; }
        public string UserProfileImage { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<MediaDto> Media { get; set; }
    }

    public class MediaDto
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public MediaType Type { get; set; }
        public string ThumbnailUrl { get; set; }
        public bool IsMain { get; set; }
    }

    public class CreateProductModel
    {
        [Required]
        public int UserId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public int? SubCategoryId { get; set; }

        [Required]
        public double Quantity { get; set; }

        [Required, StringLength(20)]
        public string Unit { get; set; }

        [Required]
        public decimal PricePerUnit { get; set; }

        public string FieldSize { get; set; }

        public int? LocationId { get; set; }

        public string Province { get; set; }
        public string District { get; set; }
        public string Village { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string Description { get; set; }
        public DateTime? HarvestDate { get; set; }
    }

    public class UpdateProductModel
    {
        public string Name { get; set; }
        public int? CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public double? Quantity { get; set; }
        public string Unit { get; set; }
        public decimal? PricePerUnit { get; set; }
        public string FieldSize { get; set; }
        public string Description { get; set; }
        public DateTime? HarvestDate { get; set; }
        public ProductStatus? Status { get; set; }
    }

    public class ProductFilterModel
    {
        public int? CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Village { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public double? MinQuantity { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Radius { get; set; } // km cinsinden yarıçap
        public DateTime? MinHarvestDate { get; set; }
        public DateTime? MaxHarvestDate { get; set; }
        public double? MinRating { get; set; }
        public bool HasVideo { get; set; }
        public bool OnlyAvailable { get; set; } = true;
        public string SortBy { get; set; } = "date_desc"; // price_asc, price_desc, date_desc, rating_desc
        public int? Page { get; set; } = 1;
        public int? PageSize { get; set; } = 10;
    }
}