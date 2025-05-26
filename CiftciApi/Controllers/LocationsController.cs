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
    public class LocationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LocationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Locations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Location>>> GetLocations()
        {
            return await _context.Locations.ToListAsync();
        }

        // GET: api/Locations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Location>> GetLocation(int id)
        {
            var location = await _context.Locations.FindAsync(id);

            if (location == null)
            {
                return NotFound();
            }

            return location;
        }

        // GET: api/Locations/Provinces
        [HttpGet("Provinces")]
        public async Task<ActionResult<IEnumerable<string>>> GetProvinces()
        {
            return await _context.Locations
                .Select(l => l.Province)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();
        }

        // GET: api/Locations/Districts/{province}
        [HttpGet("Districts/{province}")]
        public async Task<ActionResult<IEnumerable<string>>> GetDistricts(string province)
        {
            return await _context.Locations
                .Where(l => l.Province == province)
                .Select(l => l.District)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();
        }

        // GET: api/Locations/Villages/{province}/{district}
        [HttpGet("Villages/{province}/{district}")]
        public async Task<ActionResult<IEnumerable<string>>> GetVillages(string province, string district)
        {
            return await _context.Locations
                .Where(l => l.Province == province && l.District == district)
                .Where(l => l.Village != null)
                .Select(l => l.Village)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();
        }

        // POST: api/Locations
        [HttpPost]
        public async Task<ActionResult<Location>> CreateLocation(Location location)
        {
            // Aynı konumun daha önce eklenip eklenmediğini kontrol et
            var existingLocation = await _context.Locations
                .FirstOrDefaultAsync(l =>
                    l.Province == location.Province &&
                    l.District == location.District &&
                    l.Village == location.Village);

            if (existingLocation != null)
            {
                return CreatedAtAction("GetLocation", new { id = existingLocation.Id }, existingLocation);
            }

            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetLocation", new { id = location.Id }, location);
        }

        // PUT: api/Locations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLocation(int id, Location location)
        {
            if (id != location.Id)
            {
                return BadRequest();
            }

            _context.Entry(location).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LocationExists(id))
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

        // DELETE: api/Locations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null)
            {
                return NotFound();
            }

            // Bu lokasyonu kullanan kullanıcı veya ürün var mı kontrol et
            var hasUsers = await _context.Users.AnyAsync(u => u.LocationId == id);
            var hasProducts = await _context.Products.AnyAsync(p => p.LocationId == id);

            if (hasUsers || hasProducts)
            {
                return BadRequest("Bu konum kullanımda olduğu için silinemez.");
            }

            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Locations/Search
        [HttpGet("Search")]
        public async Task<ActionResult<IEnumerable<LocationSearchResult>>> SearchLocations([FromQuery] string term, int limit = 10)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2)
            {
                return BadRequest("Arama terimi en az 2 karakter olmalıdır.");
            }

            var results = new List<LocationSearchResult>();

            // İl araması
            var provinces = await _context.Locations
                .Where(l => l.Province.Contains(term))
                .Select(l => l.Province)
                .Distinct()
                .Take(limit)
                .ToListAsync();

            foreach (var province in provinces)
            {
                results.Add(new LocationSearchResult
                {
                    Text = province,
                    Type = "province"
                });
            }

            // İlçe araması
            var districts = await _context.Locations
                .Where(l => l.District.Contains(term))
                .Select(l => new { l.Province, l.District })
                .Distinct()
                .Take(limit)
                .ToListAsync();

            foreach (var district in districts)
            {
                results.Add(new LocationSearchResult
                {
                    Text = $"{district.District}, {district.Province}",
                    Type = "district"
                });
            }

            // Köy araması
            var villages = await _context.Locations
                .Where(l => l.Village != null && l.Village.Contains(term))
                .Select(l => new { l.Province, l.District, l.Village })
                .Distinct()
                .Take(limit)
                .ToListAsync();

            foreach (var village in villages)
            {
                results.Add(new LocationSearchResult
                {
                    Text = $"{village.Village}, {village.District}, {village.Province}",
                    Type = "village"
                });
            }

            return results.Take(limit).ToList();
        }

        private bool LocationExists(int id)
        {
            return _context.Locations.Any(e => e.Id == id);
        }
    }

    public class LocationSearchResult
    {
        public string Text { get; set; }
        public string Type { get; set; } // "province", "district", "village"
    }
}