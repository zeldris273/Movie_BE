using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.Data;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly MovieDbContext _context;

        public SearchController(MovieDbContext context)
        {
            _context = context;
        }

        [HttpGet("all")]
        public async Task<IActionResult> SearchAll([FromQuery] string? title)
        {
            try
            {
                if (string.IsNullOrEmpty(title))
                {
                    return BadRequest(new { message = "Vui lòng cung cấp từ khóa tìm kiếm." });
                }

                // Tìm kiếm movies
                var moviesQuery = _context.Movies
                    .Where(m => m.Title.ToLower().Contains(title.ToLower()))
                    .Select(m => new SearchResultDTO
                    {
                        Id = m.Id,
                        Type = "Movie",
                        Title = m.Title,
                        ReleaseDate = m.ReleaseDate,
                        Rating = (double?)m.Rating,
                        PosterUrl = m.PosterUrl, // Sửa từ ImageUrl thành PosterUrl
                        BackdropUrl = m.BackdropUrl
                    });

                // Tìm kiếm TV series
                var tvSeriesQuery = _context.TvSeries
                    .Where(t => t.Title.ToLower().Contains(title.ToLower()))
                    .Select(t => new SearchResultDTO
                    {
                        Id = t.Id,
                        Type = "TvSeries",
                        Title = t.Title,
                        ReleaseDate = t.ReleaseDate,
                        Rating = (double?)t.Rating,
                        PosterUrl = t.PosterUrl, // Sửa từ ImageUrl thành PosterUrl
                        BackdropUrl = t.BackdropUrl
                    });

                // Gộp query
                var combinedQuery = moviesQuery.Concat(tvSeriesQuery);

                var results = await combinedQuery.ToListAsync();

                if (!results.Any())
                {
                    return NotFound(new { message = "Không tìm thấy movie hoặc TV series nào phù hợp." });
                }

                return Ok(new { results });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Đã xảy ra lỗi khi tìm kiếm.", details = ex.Message });
            }
        }
    }
}