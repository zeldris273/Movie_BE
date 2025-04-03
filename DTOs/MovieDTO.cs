using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace backend.DTOs
{
    public class MovieDTO
    {
        public string Title { get; set; }
        public string Overview { get; set; }
        public List<string> Genres { get; set; }
        public string Status { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string Type { get; set; }
        public string Studio { get; set; }
        public string Director { get; set; }
        public IFormFile VideoFile { get; set; }
        public List<IFormFile> ImageFiles { get; set; }
    }
}