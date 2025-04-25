using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using backend.Data;
using backend.DTOs; // Import namespace của RatingDTO
using System;
using System.Linq;
using Movie_BE.Models;
using Movie_BE.DTOs; // Import namespace của Rating

namespace backend.Controllers
{
    [Route("api/ratings")]
    [ApiController]
    public class RatingController : ControllerBase
    {
        private readonly MovieDbContext _context;

        public RatingController(MovieDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubmitRating([FromBody] RatingDTO ratingDto)
        {
            // Kiểm tra user ID từ token
            var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "User not authenticated or invalid user ID" });
            }

            // Kiểm tra giá trị rating
            if (ratingDto.Rating < 1 || ratingDto.Rating > 10)
            {
                return BadRequest(new { error = "Rating must be between 1 and 10." });
            }

            // Kiểm tra media type
            if (ratingDto.MediaType != "movie" && ratingDto.MediaType != "tv")
            {
                return BadRequest(new { error = "Invalid Media Type. MediaType must be 'movie' or 'tv' and cannot be null or empty." });
            }

            // Kiểm tra xem user đã đánh giá media này chưa
            var existingRating = _context.Ratings
                .FirstOrDefault(r => r.UserId == userId && r.MediaId == ratingDto.MediaId && r.MediaType == ratingDto.MediaType);
            if (existingRating != null)
            {
                return BadRequest(new { error = "You have already rated this media." });
            }

            try
            {
                // Thêm đánh giá mới
                var rating = new Rating
                {
                    UserId = userId,
                    MediaId = ratingDto.MediaId,
                    MediaType = ratingDto.MediaType,
                    RatingValue = ratingDto.Rating,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Ratings.Add(rating);
                await _context.SaveChangesAsync();

                // Tính lại rating trung bình và số lượng đánh giá
                var ratings = _context.Ratings
                    .Where(r => r.MediaId == ratingDto.MediaId && r.MediaType == ratingDto.MediaType)
                    .ToList();

                var averageRating = ratings.Any() ? ratings.Average(r => r.RatingValue) : 0;
                var numberOfRatings = ratings.Count;

                // Cập nhật rating trung bình và số lượng đánh giá vào bảng Movies hoặc TvSeries
                if (ratingDto.MediaType == "movie")
                {
                    var movie = _context.Movies.FirstOrDefault(m => m.Id == ratingDto.MediaId);
                    if (movie == null)
                    {
                        return NotFound(new { error = "Movie not found." });
                    }
                    movie.Rating = (decimal)averageRating;
                    movie.NumberOfRatings = numberOfRatings;
                    _context.Movies.Update(movie);
                }
                else
                {
                    var tvSeries = _context.TvSeries.FirstOrDefault(t => t.Id == ratingDto.MediaId);
                    if (tvSeries == null)
                    {
                        return NotFound(new { error = "TV Series not found." });
                    }
                    tvSeries.Rating = (decimal)averageRating;
                    tvSeries.NumberOfRatings = numberOfRatings;
                    _context.TvSeries.Update(tvSeries);
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Rating submitted successfully.", averageRating });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while submitting the rating.", details = ex.Message });
            }
        }
    }
}