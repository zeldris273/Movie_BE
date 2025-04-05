using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.DTOs
{
    public class SeasonDTO
    {
        public int TvSeriesId { get; set; }
        public int SeasonNumber { get; set; }
    }

     public class SeasonResponseDTO
    {
        public int Id { get; set; }
        public int TvSeriesId { get; set; }
        public int SeasonNumber { get; set; }
    }
}