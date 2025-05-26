using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movie_BE.Models
{
    public class MovieSearchCriteria
    {
        public string[] MovieTitles { get; set; }
        public string Genre { get; set; }
        public string Year { get; set; }
        public string Actors { get; set; }
        public string Themes { get; set; }
        public string Keywords { get; set; }
    }
}