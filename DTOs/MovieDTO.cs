using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

public class MovieDTO
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Title is required")]
    public string Title { get; set; }

    public string Overview { get; set; }

    public string Genres { get; set; }

    [Required(ErrorMessage = "Status is required")]
    public string Status { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public string Studio { get; set; }

    public string Director { get; set; }

    public string PosterUrl { get; set; } // Tùy chọn (đổi tên từ ImageUrl)

    public string BackdropUrl { get; set; } // Tùy chọn

    public string VideoUrl { get; set; } // Tùy chọn

    public string TrailerUrl { get; set; } // Tùy chọn
}

public class MovieUploadDTO
{
    [Required(ErrorMessage = "Title is required")]
    public string Title { get; set; }

    public string Overview { get; set; }

    public string Genres { get; set; }

    [Required(ErrorMessage = "Status is required")]
    public string Status { get; set; }

    public DateTime? ReleaseDate { get; set; }

    [Required(ErrorMessage = "Type is required")]
    public string Type { get; set; }

    public string Studio { get; set; }

    public string Director { get; set; }

    [Required(ErrorMessage = "VideoFile is required")]
    public IFormFile VideoFile { get; set; }

    [Required(ErrorMessage = "PosterFile is required")]
    public IFormFile PosterFile { get; set; } // Đổi tên từ PosterImageFile

    [Required(ErrorMessage = "BackdropFile is required")]
    public IFormFile BackdropFile { get; set; } // Đổi tên từ BackdropImageFile
}

   public class MovieResponseDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public double? Rating { get; set; }
        public string Overview { get; set; }
        public string Genres { get; set; }
        public string Status { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string Studio { get; set; }
        public string Director { get; set; }
        public string VideoUrl { get; set; }

        public string PosterUrl { get; set; }
        public string BackdropUrl { get; set; }
        public string TrailerUrl { get; set; }
    }