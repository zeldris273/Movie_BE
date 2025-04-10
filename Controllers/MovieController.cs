using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models;
using backend.Data;
using backend.DTOs;
using backend.Services;

namespace backend.Controllers
{
    [Route("api/movies")]
    [ApiController]
    public class MovieController : ControllerBase
    {
        private readonly MovieDbContext _context;
        private readonly S3Service _s3Service;

        public MovieController(MovieDbContext context, S3Service s3Service)
        {
            _context = context;
            _s3Service = s3Service;
        }

          [HttpPost("create")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromForm] MovieDTO model)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrEmpty(model.Title))
                    return BadRequest(new { error = "Title is required" });
                if (string.IsNullOrEmpty(model.Status))
                    return BadRequest(new { error = "Status is required" });
                if (string.IsNullOrEmpty(model.Type))
                    return BadRequest(new { error = "Type is required" });
                if (model.VideoFile == null)
                    return BadRequest(new { error = "VideoFile is required" });

                // Chỉ cho phép type là "single_movie"
                if (model.Type != "single_movie")
                {
                    return BadRequest(new { error = "This endpoint only supports uploading single movies" });
                }

                if (model.ImageFiles == null)
                    return BadRequest(new { error = "ImageFiles are required for single movies" });
                if (model.ImageFiles.Count != 2)
                    return BadRequest(new { error = $"Exactly 2 images are required for single movies, but {model.ImageFiles.Count} were provided" });

                // Upload video to S3 using S3Service
                string videoFolder = $"movies/{model.Title}";
                string videoUrl = await _s3Service.UploadFileAsync(model.VideoFile, videoFolder);

                // Upload images to S3 using S3Service
                List<string> imageUrls = new List<string>();
                foreach (var image in model.ImageFiles)
                {
                    string imageUrl = await _s3Service.UploadFileAsync(image, videoFolder);
                    imageUrls.Add(imageUrl);
                }

                // Lưu phim lẻ vào cơ sở dữ liệu
                var movie = new Movie
                {
                    Title = model.Title,
                    Overview = model.Overview,
                    Genres = string.Join(",", model.Genres),
                    Status = model.Status,
                    ReleaseDate = model.ReleaseDate,
                    Studio = model.Studio,
                    Director = model.Director,
                    ImageUrl = imageUrls.FirstOrDefault(),
                    VideoUrl = videoUrl
                };
                _context.Movies.Add(movie);
                await _context.SaveChangesAsync();

                return Ok(new { videoUrl, imageUrls });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Upload failed", details = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetAllMovies()
        {
            var movies = _context.Movies.ToList();
            return Ok(movies);
        }

        [HttpGet("{id}")]
        public IActionResult GetMovie(int id)
        {
            var movie = _context.Movies.Find(id);
            if (movie == null) return NotFound();
            return Ok(movie);
        }


        [HttpPut("{id}")]
        public IActionResult UpdateMovie(int id, [FromBody] Movie updatedMovie)
        {
            var movie = _context.Movies.Find(id);
            if (movie == null) return NotFound();

            movie.Title = updatedMovie.Title;
            movie.Overview = updatedMovie.Overview;
            movie.Genres = updatedMovie.Genres;
            movie.Status = updatedMovie.Status;
            movie.ReleaseDate = updatedMovie.ReleaseDate;
            movie.Studio = updatedMovie.Studio;
            movie.Director = updatedMovie.Director;
            movie.ImageUrl = updatedMovie.ImageUrl;
            movie.VideoUrl = updatedMovie.VideoUrl;

            _context.SaveChanges();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteMovie(int id)
        {
            var movie = _context.Movies.Find(id);
            if (movie == null) return NotFound();

            _context.Movies.Remove(movie);
            _context.SaveChanges();
            return NoContent();
        }

      
    }
}