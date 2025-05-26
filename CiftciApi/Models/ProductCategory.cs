using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace CiftciApi.Models
{
    public class ProductCategory
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        public string IconUrl { get; set; }

        public string Description { get; set; }

        public virtual ICollection<ProductSubCategory> SubCategories { get; set; }
        public virtual ICollection<Product> Products { get; set; }
    }
}