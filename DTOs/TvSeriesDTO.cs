using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Movie_BE.DTOs;

namespace backend.DTOs
{
    public class TvSeriesDTO
    {
        public string Title { get; set; }
        public string Overview { get; set; }
        public string Genres { get; set; }
        public string Status { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string Studio { get; set; }
        public string Director { get; set; }
    }

    public class TvSeriesResponseDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public double? Rating { get; set; }
        public int? NumberOfRatings { get; set; }
        public string Overview { get; set; }
        public string Genres { get; set; }
        public string Status { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string Studio { get; set; }
        public string Director { get; set; }
        public string PosterUrl { get; set; }
        public string BackdropUrl { get; set; }
        public string TrailerUrl { get; set; }
        public List<ActorDTO> Actors { get; set; }
    }
    
    public class TvSeriesUploadDTO
    {
        public string Title { get; set; }
        public string Overview { get; set; }
        public List<string> Genres { get; set; }
        public string Status { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string Studio { get; set; }
        public string Director { get; set; }
        public string Actors { get; set; }
        public IFormFile PosterImageFile { get; set; } // Ảnh poster
        public IFormFile BackdropImageFile { get; set; } // Ảnh backdrop
    }
}