using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movie_BE.DTOs
{
    public class RatingDTO
    {
        public int MediaId { get; set; }
        public string MediaType { get; set; }
        public int Rating { get; set; }
    }
}