using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models;

namespace Movie_BE.Models
{
    public class WatchList 
    {
        public int Id { get; set; } // Primary key
        public int UserId { get; set; } // ID của user (liên kết với bảng Users)
        public int MediaId { get; set; } // ID của movie hoặc TV series
        public string MediaType { get; set; } // "movie" hoặc "tv"
        public DateTime AddedDate { get; set; }

        public User User { get; set; }
    }
}   