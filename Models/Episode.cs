using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Episode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Season")]
        public int SeasonId { get; set; }

        public int EpisodeNumber { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public int Duration { get; set; } // Số phút

        public DateTime? ReleaseDate { get; set; }

        public string ImageUrl { get; set; }

        public string VideoUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public Season Season { get; set; }
    }
}
