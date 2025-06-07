using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models;

namespace Movie_BE.Models
{
    public class TvSeriesActor
    {
        public int Id { get; set; }
        public int TvSeriesId { get; set; }
        public TvSeries TvSeries { get; set; }
        public int ActorId { get; set; }
        public Actor Actor { get; set; }
        public string CharacterName { get; set; }
    }
}