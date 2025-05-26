using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CiftciApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace CiftciApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public MediaController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/Media/ByProduct/5
        [HttpGet("ByProduct/{productId}")]
        public async Task<ActionResult<IEnumerable<Media>>> GetMediaByProduct(int productId)
        {
            return await _context.Media
                .Where(m => m.ProductId == productId)
                .ToListAsync();
        }

        // POST: api/Media/Upload
        [HttpPost("Upload")]
        public async Task<ActionResult<Media>> UploadMedia(IFormFile file, [FromForm] int productId, [FromForm] MediaType mediaType)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Dosya yüklenemedi.");
            }

            // Ürün kontrolü
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return BadRequest("Belirtilen ürün bulunamadı.");
            }

            // Dosya uzantısını kontrol et
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string[] allowedExtensions;

            if (mediaType == MediaType.Image)
            {
                allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            }
            else if (mediaType == MediaType.Video)
            {
                allowedExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv" };
            }
            else
            {
                return BadRequest("Geçersiz medya türü.");
            }

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest($"Desteklenmeyen dosya uzantısı. Desteklenen uzantılar: {string.Join(", ", allowedExtensions)}");
            }

            // Dosya adını oluştur
            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadFolder = "";

            if (mediaType == MediaType.Image)
            {
                uploadFolder = Path.Combine(_environment.WebRootPath, "images", "products");
            }
            else if (mediaType == MediaType.Video)
            {
                uploadFolder = Path.Combine(_environment.WebRootPath, "videos", "products");
            }

            // Klasörü oluştur (yoksa)
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var filePath = Path.Combine(uploadFolder, fileName);
            var fileUrl = $"/{(mediaType == MediaType.Image ? "images" : "videos")}/products/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Thumbnail oluştur (opsiyonel, gerçek uygulamada kullanılır)
            string thumbnailUrl = null;
            if (mediaType == MediaType.Video)
            {
                // Video için thumbnail oluşturma - pratikte FFmpeg gibi bir kütüphane kullanılır
                thumbnailUrl = $"/images/products/thumbnails/thumb_{fileName.Replace(extension, ".jpg")}";
            }

            // İlk yüklenen medya ana medya olsun
            bool isMain = !await _context.Media.AnyAsync(m => m.ProductId == productId);

            var media = new Media
            {
                ProductId = productId,
                Url = fileUrl,
                Type = mediaType,
                ThumbnailUrl = thumbnailUrl,
                IsMain = isMain,
                CreatedAt = DateTime.Now
            };

            _context.Media.Add(media);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMediaByProduct", new { productId = productId }, media);
        }

        // PUT: api/Media/SetMain/5
        [HttpPut("SetMain/{id}")]
        public async Task<IActionResult> SetMainMedia(int id)
        {
            var media = await _context.Media.FindAsync(id);
            if (media == null)
            {
                return NotFound();
            }

            // Diğer tüm medyaları ana olmayan yap
            var allMedia = await _context.Media
                .Where(m => m.ProductId == media.ProductId)
                .ToListAsync();

            foreach (var m in allMedia)
            {
                m.IsMain = (m.Id == id);
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Media/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMedia(int id)
        {
            var media = await _context.Media.FindAsync(id);
            if (media == null)
            {
                return NotFound();
            }

            // Fiziksel dosyayı sil (varsayımsal olarak)
            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, media.Url.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                if (!string.IsNullOrEmpty(media.ThumbnailUrl))
                {
                    var thumbnailPath = Path.Combine(_environment.WebRootPath, media.ThumbnailUrl.TrimStart('/'));
                    if (System.IO.File.Exists(thumbnailPath))
                    {
                        System.IO.File.Delete(thumbnailPath);
                    }
                }
            }
            catch (IOException)
            {
                // Dosya silme hatalarını logla
            }

            _context.Media.Remove(media);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}