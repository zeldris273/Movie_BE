using System;
using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class Movie
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Overview { get; set; }

        public string Genres { get; set; } // JSON hoặc chuỗi phân tách bằng dấu phẩy

        [Required]
        public string Status { get; set; } // Upcoming, Released, Canceled

        public DateTime? ReleaseDate { get; set; }

        public string Studio { get; set; }

        public string Director { get; set; }

        public string ImageUrl { get; set; } // Ảnh đại diện (poster)
        public double? Rating { get; set; }

        public string VideoUrl { get; set; } // Link video từ S3
    }
}
