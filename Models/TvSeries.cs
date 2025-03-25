using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models
{
    public class TvSeries
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Movie")]
        public int MovieId { get; set; }

        public int TotalSeasons { get; set; }
        public int TotalEpisodes { get; set; }

        public Movie Movie { get; set; }
    }
}