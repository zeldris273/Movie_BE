using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace backend.DTOs
{
    public class MovieDTO
    {
        [Required]
        public string Title { get; set; }

        public decimal Rating { get; set; }

        public string Overview { get; set; }

        public List<string> Genres { get; set; } = new List<string>();

        [Required]
        public string Status { get; set; }

        public DateTime? ReleaseDate { get; set; }

        [Required]
        public string Type { get; set; }

        public string? Studio { get; set; }

        public string? Director { get; set; }

        [Required]
        public IFormFile VideoFile { get; set; }

        [Required]
        public List<IFormFile> ImageFiles { get; set; }
    }
}