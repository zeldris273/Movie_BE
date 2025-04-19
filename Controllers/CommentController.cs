using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.DTOs;

using backend.Models;

namespace backend.Controllers
{
    [Route("api/comments")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly MovieDbContext _context;

        public CommentsController(MovieDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetComments([FromQuery] int? movieId, [FromQuery] int? tvSeriesId, [FromQuery] int? episodeId)
        {
            // Đảm bảo chỉ một trong movieId hoặc tvSeriesId được cung cấp
            if (movieId.HasValue == tvSeriesId.HasValue)
            {
                return BadRequest(new { error = "Must specify either movieId or tvSeriesId, but not both." });
            }

            var query = _context.Comments
                .Include(c => c.User)
                .Where(c => (movieId.HasValue && c.MovieId == movieId) || (tvSeriesId.HasValue && c.TvSeriesId == tvSeriesId))
                .Where(c => episodeId == null || c.EpisodeId == episodeId)
                .Select(c => new CommentResponseDTO
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    Username = c.User.Email, // Sử dụng Email vì bảng users không có Username
                    MovieId = c.MovieId,
                    TvSeriesId = c.TvSeriesId,
                    EpisodeId = c.EpisodeId,
                    CommentText = c.CommentText,
                    Timestamp = c.Timestamp
                })
                .OrderByDescending(c => c.Timestamp);

            var comments = query.ToList();
            return Ok(comments);
        }

        [HttpPost]
        public IActionResult AddComment([FromBody] CommentRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.CommentText))
            {
                return BadRequest(new { error = "Comment text cannot be empty." });
            }

            // Kiểm tra chỉ một trong movieId hoặc tvSeriesId được cung cấp
            if (request.MovieId.HasValue == request.TvSeriesId.HasValue)
            {
                return BadRequest(new { error = "Must specify either movieId or tvSeriesId, but not both." });
            }

            // Kiểm tra movie hoặc tvseries tồn tại
            if (request.MovieId.HasValue)
            {
                var movie = _context.Movies.Find(request.MovieId);
                if (movie == null)
                {
                    return NotFound(new { error = "Movie not found." });
                }

                // Nếu là movie, episodeId phải là null
                if (request.EpisodeId.HasValue)
                {
                    return BadRequest(new { error = "EpisodeId must be null for movie comments." });
                }
            }
            else if (request.TvSeriesId.HasValue)
            {
                var tvSeries = _context.TvSeries.Find(request.TvSeriesId);
                if (tvSeries == null)
                {
                    return NotFound(new { error = "TV Series not found." });
                }

                // Nếu là tvseries, episodeId phải có giá trị
                if (!request.EpisodeId.HasValue)
                {
                    return BadRequest(new { error = "EpisodeId is required for TV series comments." });
                }

                var episode = _context.Episodes.Find(request.EpisodeId);
                if (episode == null)
                {
                    return NotFound(new { error = "Episode not found." });
                }

                // Kiểm tra episode thuộc về tvseries
                var season = _context.Seasons.FirstOrDefault(s => s.Id == episode.SeasonId);
                if (season == null || season.TvSeriesId != request.TvSeriesId)
                {
                    return BadRequest(new { error = "Episode does not belong to the specified TV series." });
                }
            }

            var user = _context.Users.Find(request.UserId);
            if (user == null)
            {
                return NotFound(new { error = "User not found." });
            }

            var comment = new Comment
            {
                UserId = request.UserId,
                MovieId = request.MovieId,
                TvSeriesId = request.TvSeriesId,
                EpisodeId = request.EpisodeId,
                CommentText = request.CommentText,
                Timestamp = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Comments.Add(comment);
            _context.SaveChanges();

            var response = new CommentResponseDTO
            {
                Id = comment.Id,
                UserId = comment.UserId,
                Username = user.Email,
                MovieId = comment.MovieId,
                TvSeriesId = comment.TvSeriesId,
                EpisodeId = comment.EpisodeId,
                CommentText = comment.CommentText,
                Timestamp = comment.Timestamp
            };

            return CreatedAtAction(nameof(GetComments), new { movieId = comment.MovieId, tvSeriesId = comment.TvSeriesId, episodeId = comment.EpisodeId }, response);
        }
    }
}