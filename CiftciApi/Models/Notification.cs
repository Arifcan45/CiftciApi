using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CiftciApi.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; }

        [Required, StringLength(500)]
        public string Content { get; set; }

        public NotificationType Type { get; set; }

        public bool IsRead { get; set; } = false;

        public string RedirectUrl { get; set; }

        public int? RelatedEntityId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ReadAt { get; set; }
    }

    public enum NotificationType
    {
        NewMessage = 1,
        ProductInterest = 2,
        NewReview = 3,
        System = 4
    }
}