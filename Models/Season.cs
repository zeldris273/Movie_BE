using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Season
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("TvSeries")]
        public int TvSeriesId { get; set; }

        public int SeasonNumber { get; set; }

        public DateTime? ReleaseDate { get; set; }

        public TvSeries TvSeries { get; set; }
    }
}
