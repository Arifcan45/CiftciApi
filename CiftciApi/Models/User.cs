using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CiftciApi.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

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

        public double? Rating { get; set; }

        public int? ReviewCount { get; set; }

        public string ProfileImageUrl { get; set; }

        // Çiftçi için ek alanlar
        public int? LocationId { get; set; }
        public virtual Location Location { get; set; }

        public virtual ICollection<Product> Products { get; set; }
        public virtual ICollection<Review> ReviewsReceived { get; set; }
        public virtual ICollection<Review> ReviewsGiven { get; set; }
        public virtual ICollection<Message> MessagesSent { get; set; }
        public virtual ICollection<Message> MessagesReceived { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLoginDate { get; set; }
    }

    public enum UserType
    {
        Alici = 1,
        Ciftci = 2
    }
}