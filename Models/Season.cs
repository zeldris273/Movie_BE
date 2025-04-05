using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
   public class Season
    {
        [Key]
        public int Id { get; set; }

        public int TvSeriesId { get; set; }

        public TvSeries TvSeries { get; set; }

        public int SeasonNumber { get; set; }

        public List<Episode> Episodes { get; set; } = new List<Episode>();
    }
}
