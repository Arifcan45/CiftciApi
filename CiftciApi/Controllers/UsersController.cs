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
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            return await _context.Users
                .Include(u => u.Location)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    UserType = u.UserType,
                    Rating = u.Rating,
                    ReviewCount = u.ReviewCount,
                    ProfileImageUrl = u.ProfileImageUrl,
                    Province = u.Province ?? u.Location.Province,
                    District = u.District ?? u.Location.District,
                    Village = u.Village ?? u.Location.Village,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Location)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserType = user.UserType,
                Rating = user.Rating,
                ReviewCount = user.ReviewCount,
                ProfileImageUrl = user.ProfileImageUrl,
                Province = user.Province ?? user.Location?.Province,
                District = user.District ?? user.Location?.District,
                Village = user.Village ?? user.Location?.Village,
                CreatedAt = user.CreatedAt
            };

            return userDto;
        }

        // POST: api/Users/Register
        [HttpPost("Register")]
        public async Task<ActionResult<User>> Register(RegisterModel model)
        {
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                return Conflict("Bu e-posta adresi zaten kullanılıyor.");
            }

            // Lokasyon bilgisi varsa ekle veya güncelle
            Location location = null;
            if (!string.IsNullOrEmpty(model.Province) && !string.IsNullOrEmpty(model.District))
            {
                location = await _context.Locations
                    .FirstOrDefaultAsync(l =>
                        l.Province == model.Province &&
                        l.District == model.District &&
                        l.Village == (model.Village ?? ""));

                if (location == null && model.Latitude.HasValue && model.Longitude.HasValue)
                {
                    location = new Location
                    {
                        Province = model.Province,
                        District = model.District,
                        Village = model.Village,
                        Latitude = model.Latitude.Value,
                        Longitude = model.Longitude.Value
                    };

                    _context.Locations.Add(location);
                    await _context.SaveChangesAsync();
                }
            }

            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Password = model.Password, // Gerçek uygulamada şifre hashlenmeli
                UserType = model.UserType,
                LocationId = location?.Id,
                ProfileImageUrl = model.ProfileImageUrl,
                Province = model.Province,
                District = model.District,
                Village = model.Village,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.Id }, user);
        }

        // POST: api/Users/Login
        [HttpPost("Login")]
        public async Task<ActionResult<UserDto>> Login(LoginModel model)
        {
            var user = await _context.Users
                .Include(u => u.Location)
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);

            if (user == null)
            {
                return NotFound("E-posta adresi veya şifre hatalı.");
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserType = user.UserType,
                Rating = user.Rating,
                ReviewCount = user.ReviewCount,
                ProfileImageUrl = user.ProfileImageUrl,
                Province = user.Province ?? user.Location?.Province,
                District = user.District ?? user.Location?.District,
                Village = user.Village ?? user.Location?.Village,
                CreatedAt = user.CreatedAt
            };

            return userDto;
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserModel model)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Kullanıcı bilgilerini güncelle
            if (!string.IsNullOrEmpty(model.Name))
                user.Name = model.Name;

            if (!string.IsNullOrEmpty(model.PhoneNumber))
                user.PhoneNumber = model.PhoneNumber;

            if (!string.IsNullOrEmpty(model.ProfileImageUrl))
                user.ProfileImageUrl = model.ProfileImageUrl;

            // Lokasyon bilgisini güncelle
            if (!string.IsNullOrEmpty(model.Province))
                user.Province = model.Province;

            if (!string.IsNullOrEmpty(model.District))
                user.District = model.District;

            if (!string.IsNullOrEmpty(model.Village))
                user.Village = model.Village;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
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

        // GET: api/Users/Ciftciler
        [HttpGet("Ciftciler")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetCiftciler()
        {
            var users = await _context.Users
                .Include(u => u.Location)
                .Where(u => u.UserType == UserType.Ciftci)
                .ToListAsync();

            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                UserType = u.UserType,
                Rating = u.Rating,
                ReviewCount = u.ReviewCount,
                ProfileImageUrl = u.ProfileImageUrl,
                Province = u.Province ?? (u.Location != null ? u.Location.Province : null),
                District = u.District ?? (u.Location != null ? u.Location.District : null),
                Village = u.Village ?? (u.Location != null ? u.Location.Village : null),
                CreatedAt = u.CreatedAt
            }).ToList();

            return userDtos;
        }

        // GET: api/Users/Alicilar
        [HttpGet("Alicilar")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAlicilar()
        {
            var users = await _context.Users
                .Include(u => u.Location)
                .Where(u => u.UserType == UserType.Alici)
                .ToListAsync();

            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                UserType = u.UserType,
                Rating = u.Rating,
                ReviewCount = u.ReviewCount,
                ProfileImageUrl = u.ProfileImageUrl,
                Province = u.Province ?? (u.Location != null ? u.Location.Province : null),
                District = u.District ?? (u.Location != null ? u.Location.District : null),
                Village = u.Village ?? (u.Location != null ? u.Location.Village : null),
                CreatedAt = u.CreatedAt
            }).ToList();

            return userDtos;
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public UserType UserType { get; set; }
        public double? Rating { get; set; }
        public int? ReviewCount { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Village { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RegisterModel
    {
        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, StringLength(100), EmailAddress]
        public string Email { get; set; }

        [Required, StringLength(20)]
        public string PhoneNumber { get; set; }

        [Required, StringLength(100)]
        public string Password { get; set; }

        [Required]
        public UserType UserType { get; set; }

        public string ProfileImageUrl { get; set; }

        public string Province { get; set; }
        public string District { get; set; }
        public string Village { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class LoginModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class UpdateUserModel
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Village { get; set; }
    }
}