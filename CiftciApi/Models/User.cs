using System.ComponentModel.DataAnnotations;

namespace CiftciApi.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string ?Name { get; set; }

        [Required, StringLength(100), EmailAddress]
        public string? Email { get; set; }

        [Required, StringLength(20)]
        public string ?PhoneNumber { get; set; }

        [Required, StringLength(100)]
        public string ?Password { get; set; }

        [Required]
        public UserType UserType { get; set; }

        public double? Rating { get; set; }

        public int? ReviewCount { get; set; }

        public string? ProfileImageUrl { get; set; }

        // Çiftçi için ek alanlar
        public string? Province { get; set; } // İl
        public string? District { get; set; } // İlçe
        public string? Village { get; set; } // Köy

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum UserType
    {
        Alici = 1,
        Ciftci = 2
    }
}