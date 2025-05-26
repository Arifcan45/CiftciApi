using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CiftciApi.Models
{
    public class Media
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product ?Product { get; set; }

        [Required, StringLength(500)]
        public string ?Url { get; set; }

        [Required]
        public MediaType Type { get; set; }

        public string ?ThumbnailUrl { get; set; }

        public bool IsMain { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum MediaType
    {
        Image = 1,
        Video = 2
    }
}