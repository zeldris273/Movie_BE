using Microsoft.AspNetCore.Identity;
using Movie_BE.Models;
using System;
using System.Collections.Generic;

namespace backend.Models
{
    public class CustomUser : IdentityUser<int> // Sử dụng int làm kiểu khóa chính (thay vì string mặc định)
    {
        public DateTime CreatedAt { get; set; }
        public List<Comment> Comments { get; set; } = new List<Comment>();
        public List<WatchList> WatchList { get; set; }
        public List<Rating> Ratings { get; set; }

        public CustomUser()
        {
            CreatedAt = DateTime.UtcNow; // Gán thời gian tạo mặc định
        }
    }
}