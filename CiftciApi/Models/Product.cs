using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CiftciApi.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User ?User { get; set; }

        [Required, StringLength(100)]
        public string ?Name { get; set; }

        [Required, StringLength(100)]
        public string ?Category { get; set; }

        [StringLength(100)]
        public string ?SubCategory { get; set; }

        [Required]
        public double Quantity { get; set; }

        [Required]
        public string ?Unit { get; set; } // kg, ton, adet vb.

        [Required]
        public decimal PricePerUnit { get; set; }

        public string ?FieldSize { get; set; } // Örn: "3 dönüm alan"

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public string ?VideoUrl { get; set; }

        public string ?ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true;

        public DateTime HarvestDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
