using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models;

namespace Movie_BE.Models
{
    public class Rating
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MediaId { get; set; }
        public string MediaType { get; set; }
        public int RatingValue { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; }
    }
}