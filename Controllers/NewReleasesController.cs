using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Movie_BE.Controllers
{
    [Route("api/new-releases")]
    [ApiController]
    public class NewReleasesController : ControllerBase
    {
        private readonly MovieDbContext _context;   

        public NewReleasesController(MovieDbContext context)
        {
            _context = context;
        }

        // GET: api/new-releases
        [HttpGet]
        public async Task<IActionResult> GetNewReleases(
            [FromQuery] int limit = 10,
            [FromQuery] int offset = 0,
            [FromQuery] string? mediaType = null)
        {
            try
            {
                // Lấy phim lẻ mới nhất (status = 'Released')
                var newMovies = _context.Movies
                    .Where(m => m.Status == "Released")
                    .Select(m => new
                    {
                        Id = m.Id,
                        Title = m.Title,
                        Overview = m.Overview,
                        ViewCount = (int?)m.ViewCount,
                        Rating = (double?)m.Rating,
                        BackdropUrl = m.BackdropUrl,
                        MediaType = "movie"
                    });

                // Lấy phim bộ mới nhất (status = 'Ongoing' hoặc 'Completed')
                var newTvSeries = _context.TvSeries
                    .Where(t => t.Status == "Ongoing" || t.Status == "Completed")
                    .Select(t => new
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Overview = t.Overview,
                        ViewCount = (int?)t.ViewCount,
                        Rating = (double?)t.Rating,
                        BackdropUrl = t.BackdropUrl,
                        MediaType = "tvseries"
                    });

                // Kết hợp cả hai danh sách
                var combinedReleases = Queryable.Concat(newMovies, newTvSeries);

                // Lọc theo mediaType nếu có
                if (!string.IsNullOrEmpty(mediaType))
                {
                    if (mediaType.ToLower() == "movie" || mediaType.ToLower() == "tvseries")
                    {
                        combinedReleases = combinedReleases
                            .Where(r => r.MediaType == mediaType.ToLower());
                    }
                    else
                    {
                        return BadRequest(new { error = "Invalid mediaType. Use 'movie' or 'tvseries'." });
                    }
                }

                // Tổng số bản phát hành
                var total = await combinedReleases.CountAsync();

                // Sắp xếp theo ngày hiện tại (giả lập "mới nhất" dựa trên ViewCount hoặc Rating nếu ReleaseDate không phù hợp)
                // Lưu ý: Nếu bạn muốn dùng ReleaseDate, cần đảm bảo dữ liệu có giá trị
                var releases = await combinedReleases
                    .OrderByDescending(r => r.ViewCount) // Sắp xếp theo lượt xem (có thể thay bằng ReleaseDate nếu cần)
                    .ThenByDescending(r => r.Rating)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();

                return Ok(new
                {
                    Data = releases,
                    Total = total,
                    Limit = limit,
                    Offset = offset
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Internal server error", Details = ex.Message });
            }
        }
    }
}