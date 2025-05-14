using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movie_BE.Models
{
    public class Actor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
        public List<TvSeriesActor> TvSeriesActors { get; set; } = new List<TvSeriesActor>();
    }
}