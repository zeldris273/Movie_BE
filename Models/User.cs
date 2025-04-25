using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Movie_BE.Models;

namespace backend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<Comment> Comments { get; set; } = new List<Comment>();
        public List<WatchList> WatchList { get; set; }
    }
}