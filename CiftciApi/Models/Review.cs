using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CiftciApi.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReviewerId { get; set; }

        [ForeignKey("ReviewerId")]
        public virtual User Reviewer { get; set; }

        [Required]
        public int ReviewedUserId { get; set; }

        [ForeignKey("ReviewedUserId")]
        public virtual User ReviewedUser { get; set; }

        [Required]
        public int Rating { get; set; } // 1-5

        [StringLength(500)]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}