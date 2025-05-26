using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace CiftciApi.Models
{
    public class Location
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Province { get; set; } // İl

        [Required, StringLength(100)]
        public string District { get; set; } // İlçe

        [StringLength(100)]
        public string Village { get; set; } // Köy

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public virtual ICollection<User> Users { get; set; }
        public virtual ICollection<Product> Products { get; set; }
    }
}
