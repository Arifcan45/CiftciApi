using System;
using System.Collections.Generic;
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
        public virtual User User { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual ProductCategory Category { get; set; }

        public int? SubCategoryId { get; set; }

        [ForeignKey("SubCategoryId")]
        public virtual ProductSubCategory SubCategory { get; set; }

        [Required]
        public double Quantity { get; set; }

        [Required, StringLength(20)]
        public string Unit { get; set; } // kg, ton, adet vb.

        [Required]
        public decimal PricePerUnit { get; set; }

        [StringLength(100)]
        public string FieldSize { get; set; } // Örn: "3 dönüm alan"

        public int? LocationId { get; set; }

        [ForeignKey("LocationId")]
        public virtual Location Location { get; set; }

        public virtual ICollection<Media> Media { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Available;

        public DateTime? HarvestDate { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }

    public enum ProductStatus
    {
        Available = 1,
        Reserved = 2,
        Sold = 3
    }
}