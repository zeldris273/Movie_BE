using System;
using System.ComponentModel.DataAnnotations;
using Movie_BE.Models;

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

        public string PosterUrl { get; set; } // Ảnh đại diện (poster)
        public string BackdropUrl { get; set; } // Ảnh đại diện (poster)
        public decimal? Rating { get; set; }
        public int? NumberOfRatings { get; set; } 

        public string VideoUrl { get; set; } // Link video từ S3
        public string? TrailerUrl { get; set; } // Link video từ S3
        public int ViewCount { get; set; }
        public List<Comment> Comments { get; set; } = new List<Comment>();
        public List<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
    }
}
