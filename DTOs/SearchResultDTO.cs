using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models
{
    public class SearchResultDTO
    {
        public int Id { get; set; }
        public string Type { get; set; } // "Movie" hoặc "TvSeries"
        public string Title { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public double? Rating { get; set; }
        public string ImageUrl { get; set; }
    }
}