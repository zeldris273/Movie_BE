using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.Data;
using backend.Services;
using System.Text.RegularExpressions;

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
        public async Task<IActionResult> Create([FromForm] MovieUploadDTO model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Title))
                    return BadRequest(new { error = "Title is required" });
                if (string.IsNullOrEmpty(model.Status))
                    return BadRequest(new { error = "Status is required" });
                if (string.IsNullOrEmpty(model.Type))
                    return BadRequest(new { error = "Type is required" });
                if (model.VideoFile == null)
                    return BadRequest(new { error = "VideoFile is required" });

                if (model.Type != "single_movie")
                    return BadRequest(new { error = "This endpoint only supports uploading single movies" });

                var validStatuses = new[] { "Upcoming", "Released", "Canceled" };
                if (!validStatuses.Contains(model.Status))
                    return BadRequest(new { error = "Invalid Status. Must be 'Upcoming', 'Released', or 'Canceled'." });

                if (model.BackdropFile == null || model.PosterFile == null)
                    return BadRequest(new { error = "Backdrop and Poster images are required for single movies" });

                var validVideoExtensions = new[] { ".mp4", ".avi", ".mov", ".ts" };
                var validImageExtensions = new[] { ".jpg", ".jpeg", ".png" };

                if (!validVideoExtensions.Contains(Path.GetExtension(model.VideoFile.FileName).ToLower()))
                    return BadRequest(new { error = "VideoFile must be .mp4, .avi, .mov, or .ts" });

                if (!validImageExtensions.Contains(Path.GetExtension(model.BackdropFile.FileName).ToLower()))
                    return BadRequest(new { error = "BackdropFile must be .jpg, .jpeg, or .png" });

                if (!validImageExtensions.Contains(Path.GetExtension(model.PosterFile.FileName).ToLower()))
                    return BadRequest(new { error = "PosterFile must be .jpg, .jpeg, or .png" });

                string videoFolder = $"movies/{model.Title}";
                string videoUrl = await _s3Service.UploadFileAsync(model.VideoFile, videoFolder);

                List<string> imageUrls = new List<string>();
                string backdropUrl = await _s3Service.UploadFileAsync(model.BackdropFile, videoFolder);
                string posterUrl = await _s3Service.UploadFileAsync(model.PosterFile, videoFolder);
                imageUrls.Add(backdropUrl);
                imageUrls.Add(posterUrl);

                var movie = new Movie
                {
                    Title = model.Title,
                    Overview = model.Overview,
                    Genres = model.Genres,
                    Status = model.Status,
                    ReleaseDate = model.ReleaseDate,
                    Studio = model.Studio,
                    Director = model.Director,
                    PosterUrl = posterUrl,
                    BackdropUrl = backdropUrl,
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
            var result = movies.Select(m => new MovieResponseDTO
            {
                Id = m.Id,
                Title = m.Title,
                Overview = m.Overview,
                Genres = m.Genres,
                Status = m.Status,
                Rating = (double?)m.Rating,
                NumberOfRatings = m.NumberOfRatings,
                ReleaseDate = m.ReleaseDate,
                Studio = m.Studio,
                Director = m.Director,
                PosterUrl = m.PosterUrl,
                BackdropUrl = m.BackdropUrl,
                VideoUrl = m.VideoUrl,
                TrailerUrl = m.TrailerUrl
            });
            return Ok(result);
        }


        // Endpoint cho URL xem chi tiết: /api/movies/{id}/{title}
        [HttpGet("{id}/{title}")]
        public IActionResult GetMovie(int id, string title)
        {
            var movie = _context.Movies.Find(id);
            if (movie == null) return NotFound(new { error = "Movie not found" });

            string expectedSlug = movie.Title.ToLower()
         .Replace(" ", "-") // Thay khoảng trắng bằng dấu gạch ngang
         .Trim(); // Bỏ khoảng trắng thừa
            expectedSlug = Regex.Replace(expectedSlug, "[^a-z0-9-]", ""); // Bỏ ký tự không phải chữ, số, hoặc dấu gạch ngang
            expectedSlug = Regex.Replace(expectedSlug, "-+", "-");


            if (title != expectedSlug)
            {
                return NotFound(new { error = "Movie not found" });
            }

            var result = new MovieResponseDTO
            {
                Id = movie.Id,
                Title = movie.Title,
                Overview = movie.Overview,
                Genres = movie.Genres,
                Status = movie.Status,
                Rating = (double?)movie.Rating,
                NumberOfRatings = movie.NumberOfRatings,
                ReleaseDate = movie.ReleaseDate,
                Studio = movie.Studio,
                Director = movie.Director,
                PosterUrl = movie.PosterUrl,
                BackdropUrl = movie.BackdropUrl,
                VideoUrl = movie.VideoUrl,
                TrailerUrl = movie.TrailerUrl
            };
            return Ok(result);
        }

        [HttpGet("{id}/{title}/watch")]
        public async Task<IActionResult> WatchMovie(int id, string title)
        {
            var movie = _context.Movies.Find(id);
            if (movie == null) return NotFound(new { error = "Movie not found" });

            // Kiểm tra slug của title
            string expectedSlug = movie.Title.ToLower()
                .Replace(" ", "-")
                .Trim();
            expectedSlug = Regex.Replace(expectedSlug, "[^a-z0-9-]", "");
            expectedSlug = Regex.Replace(expectedSlug, "-+", "-");

            if (title != expectedSlug)
            {
                return NotFound(new { error = "Movie not found" });
            }

            if (string.IsNullOrEmpty(movie.VideoUrl))
                return BadRequest(new { error = "Video URL not available for this movie" });

            movie.ViewCount += 1;
            await _context.SaveChangesAsync();

            return Ok(new { videoUrl = movie.VideoUrl });
        }

        [HttpPut("{id}")]
        public IActionResult UpdateMovie(int id, [FromBody] MovieDTO updatedMovie)
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
            movie.PosterUrl = updatedMovie.PosterUrl;
            movie.BackdropUrl = updatedMovie.BackdropUrl;
            movie.VideoUrl = updatedMovie.VideoUrl;
            movie.TrailerUrl = updatedMovie.TrailerUrl;

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

        [HttpGet("top-rated-by-votes")]
        public async Task<IActionResult> GetTopRatedMoviesByVotes()
        {
            var topMovies = await _context.Movies
                .OrderByDescending(m => m.NumberOfRatings)
                .Take(20)
                .ToListAsync();

            return Ok(topMovies);
        }
    }
}