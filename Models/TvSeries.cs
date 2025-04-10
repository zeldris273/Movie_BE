using System;
using System.Collections.Generic;
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
        public string Status { get; set; }

        public DateTime? ReleaseDate { get; set; }

        public string Studio { get; set; }

        public string Director { get; set; }

        public string ImageUrl { get; set; } 

        public string BackdropUrl { get; set; }
        public double? Rating { get; set; }

        public List<Season> Seasons { get; set; } = new List<Season>();
    }
}