using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models
{
    public class Movie
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public decimal Rating { get; set; }

        public string Overview { get; set; }

        public string Genres { get; set; } // Lưu dưới dạng JSON hoặc chuỗi phân cách bởi dấu phẩy

        [Required]
        public string Status { get; set; }

        public DateTime? ReleaseDate { get; set; }

        [Required]
        public string Type { get; set; } // 'movie' hoặc 'tv_series'

        public string? Studio { get; set; }

        public string? Director { get; set; }
        public string VideoUrl { get; set; } // Đường dẫn lưu trữ video

        public string ImageUrls { get; set; } // Lưu danh sách ảnh dưới dạng JSON hoặc CSV
    }
}
