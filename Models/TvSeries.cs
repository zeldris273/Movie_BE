using System;
using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class TvSeries
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Overview { get; set; }

        public string Genres { get; set; } 

        [Required]
        public string Status { get; set; } // Ongoing, Completed, Canceled

        public DateTime? ReleaseDate { get; set; }

        public string Studio { get; set; }

        public string Director { get; set; }

        public string ImageUrl { get; set; } // Ảnh đại diện (poster)
    }
}
