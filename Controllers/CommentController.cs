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

        // Lấy danh sách bình luận (hỗ trợ trả lời dạng cây)
        [HttpGet]
        public IActionResult GetComments([FromQuery] int? movieId, [FromQuery] int? tvSeriesId, [FromQuery] int? episodeId)
        {
            if (movieId.HasValue == tvSeriesId.HasValue)
            {
                return BadRequest(new { error = "Must specify either movieId or tvSeriesId, but not both." });
            }

            // Lấy tất cả bình luận (bao gồm cả trả lời)
            var allComments = _context.Comments
                .Include(c => c.User)
                .Include(c => c.Replies) // Bao gồm các trả lời
                .Where(c => (movieId.HasValue && c.MovieId == movieId) || (tvSeriesId.HasValue && c.TvSeriesId == tvSeriesId))
                .Where(c => episodeId == null || c.EpisodeId == episodeId)
                .ToList();

            // Chỉ lấy các bình luận gốc (ParentCommentId == null)
            var rootComments = allComments
                .Where(c => c.ParentCommentId == null)
                .Select(c => MapToCommentResponseDTO(c))
                .OrderByDescending(c => c.Timestamp)
                .ToList();

            return Ok(rootComments);
        }

        // Thêm bình luận mới (hỗ trợ trả lời)
        [HttpPost]
        public IActionResult AddComment([FromBody] CommentRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.CommentText))
            {
                return BadRequest(new { error = "Comment text cannot be empty." });
            }

            if (request.MovieId.HasValue == request.TvSeriesId.HasValue)
            {
                return BadRequest(new { error = "Must specify either movieId or tvSeriesId, but not both." });
            }

            if (request.MovieId.HasValue)
            {
                var movie = _context.Movies.Find(request.MovieId);
                if (movie == null)
                {
                    return NotFound(new { error = "Movie not found." });
                }

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

                if (request.EpisodeId.HasValue)
                {
                    var episode = _context.Episodes.Find(request.EpisodeId);
                    if (episode == null)
                    {
                        return NotFound(new { error = "Episode not found." });
                    }

                    var season = _context.Seasons.FirstOrDefault(s => s.Id == episode.SeasonId);
                    if (season == null || season.TvSeriesId != request.TvSeriesId)
                    {
                        return BadRequest(new { error = "Episode does not belong to the specified TV series." });
                    }
                }
            }

            var user = _context.Users.Find(request.UserId);
            if (user == null)
            {
                return NotFound(new { error = "User not found." });
            }

            // Kiểm tra ParentCommentId (nếu là trả lời)
            if (request.ParentCommentId.HasValue)
            {
                var parentComment = _context.Comments.Find(request.ParentCommentId);
                if (parentComment == null)
                {
                    return NotFound(new { error = "Parent comment not found." });
                }

                // Đảm bảo trả lời thuộc cùng movie hoặc tvSeries
                if (parentComment.MovieId != request.MovieId || parentComment.TvSeriesId != request.TvSeriesId)
                {
                    return BadRequest(new { error = "Parent comment does not belong to the specified movie or TV series." });
                }

                // Đảm bảo trả lời cùng episode (nếu có)
                if (parentComment.EpisodeId != request.EpisodeId)
                {
                    return BadRequest(new { error = "Parent comment does not belong to the specified episode." });
                }
            }

            var comment = new Comment
            {
                UserId = request.UserId,
                MovieId = request.MovieId,
                TvSeriesId = request.TvSeriesId,
                EpisodeId = request.EpisodeId,
                ParentCommentId = request.ParentCommentId,
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
                ParentCommentId = comment.ParentCommentId,
                CommentText = comment.CommentText,
                Timestamp = comment.Timestamp
            };

            return CreatedAtAction(nameof(GetComments), new { movieId = comment.MovieId, tvSeriesId = comment.TvSeriesId, episodeId = comment.EpisodeId }, response);
        }

        // Sửa bình luận
        [HttpPut("{id}")]
        public IActionResult UpdateComment(int id, [FromBody] CommentRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.CommentText))
            {
                return BadRequest(new { error = "Comment text cannot be empty." });
            }

            var comment = _context.Comments
                .Include(c => c.User)
                .FirstOrDefault(c => c.Id == id);

            if (comment == null)
            {
                return NotFound(new { error = "Comment not found." });
            }

            // Kiểm tra quyền sửa (chỉ người tạo bình luận được sửa)
            if (comment.UserId != request.UserId)
            {
                return Forbid("You are not allowed to edit this comment.");
            }

            // Cập nhật nội dung bình luận
            comment.CommentText = request.CommentText;
            comment.UpdatedAt = DateTime.Now;

            _context.SaveChanges();

            var response = new CommentResponseDTO
            {
                Id = comment.Id,
                UserId = comment.UserId,
                Username = comment.User.Email,
                MovieId = comment.MovieId,
                TvSeriesId = comment.TvSeriesId,
                EpisodeId = comment.EpisodeId,
                ParentCommentId = comment.ParentCommentId,
                CommentText = comment.CommentText,
                Timestamp = comment.Timestamp
            };

            return Ok(response);
        }

        // Xóa bình luận
        [HttpDelete("{id}")]
        public IActionResult DeleteComment(int id, [FromQuery] int userId)
        {
            var comment = _context.Comments.FirstOrDefault(c => c.Id == id);
            if (comment == null)
            {
                return NotFound(new { error = "Comment not found." });
            }

            // Kiểm tra quyền xóa (chỉ người tạo bình luận được xóa)
            if (comment.UserId != userId)
            {
                return Forbid("You are not allowed to delete this comment.");
            }

            _context.Comments.Remove(comment);
            _context.SaveChanges();

            return NoContent();
        }

        // Hàm ánh xạ Comment sang CommentResponseDTO (bao gồm trả lời)
        private CommentResponseDTO MapToCommentResponseDTO(Comment comment)
        {
            var dto = new CommentResponseDTO
            {
                Id = comment.Id,
                UserId = comment.UserId,
                Username = comment.User.Email,
                MovieId = comment.MovieId,
                TvSeriesId = comment.TvSeriesId,
                EpisodeId = comment.EpisodeId,
                ParentCommentId = comment.ParentCommentId,
                CommentText = comment.CommentText,
                Timestamp = comment.Timestamp,
                Replies = comment.Replies
                    .Select(r => MapToCommentResponseDTO(r))
                    .OrderByDescending(r => r.Timestamp)
                    .ToList()
            };
            return dto;
        }
    }
}