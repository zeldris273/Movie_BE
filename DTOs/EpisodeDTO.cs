using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.DTOs
{
    public class EpisodeDTO
    {
        
    }

    public class EpisodeUploadDTO
    {
        public int TvSeriesId { get; set; }
        public int SeasonId { get; set; }
        public int EpisodeNumber { get; set; }
        public IFormFile VideoFile { get; set; }
    }

    public class EpisodeResponseDTO
    {

        public int Id { get; set; }
        public int SeasonId { get; set; }
        public int EpisodeNumber { get; set; }
        public string VideoUrl { get; set; }
    }
}