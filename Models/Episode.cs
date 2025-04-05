using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Episode
    {
        [Key]
        public int Id { get; set; }

        public int SeasonId { get; set; }

        public Season Season { get; set; }

        public int EpisodeNumber { get; set; }

        public string VideoUrl { get; set; }
    }
}
