using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models;
using backend.Data;
using backend.DTOs;
using backend.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("api/tvseries")]
    [ApiController]
    public class TvSeriesController : ControllerBase
    {
        private readonly MovieDbContext _context;
        private readonly S3Service _s3Service;

        public TvSeriesController(MovieDbContext context, S3Service s3Service)
        {
            _context = context;
            _s3Service = s3Service;
        }

        [HttpGet]
        public IActionResult GetAllTvSeries()
        {
            var series = _context.TvSeries
                .Select(s => new TvSeriesResponseDTO
                {
                    Id = s.Id,
                    Title = s.Title,
                    Overview = s.Overview,
                    Rating = (double?)s.Rating,
                    Genres = s.Genres,
                    Status = s.Status,
                    ReleaseDate = s.ReleaseDate,
                    Studio = s.Studio,
                    Director = s.Director,
                    PosterUrl = s.PosterUrl,
                    BackdropUrl = s.BackdropUrl
                })
                .ToList();
            return Ok(series);
        }

        [HttpGet("{id}")]
        public IActionResult GetTvSeries(int id)
        {
            var series = _context.TvSeries.Find(id);
            if (series == null) return NotFound();

            var response = new TvSeriesResponseDTO
            {
                Id = series.Id,
                Title = series.Title,
                Overview = series.Overview,
                Rating = (double?)series.Rating,
                NumberOfRatings = series.NumberOfRatings,
                Genres = series.Genres,
                Status = series.Status,
                ReleaseDate = series.ReleaseDate,
                Studio = series.Studio,
                Director = series.Director,
                PosterUrl = series.PosterUrl,
                BackdropUrl = series.BackdropUrl,
                TrailerUrl = series.TrailerUrl
            };
            return Ok(response);
        }

        [HttpPost("create")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateTvSeries([FromForm] TvSeriesUploadDTO model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Title))
                    return BadRequest(new { error = "Title is required" });
                if (string.IsNullOrEmpty(model.Status))
                    return BadRequest(new { error = "Status is required" });
                if (model.PosterImageFile == null)
                    return BadRequest(new { error = "PosterImageFile is required" });
                if (model.BackdropImageFile == null)
                    return BadRequest(new { error = "BackdropImageFile is required" });

                var validStatuses = new[] { "Ongoing", "Completed", "Canceled" };
                if (!validStatuses.Contains(model.Status))
                {
                    return BadRequest(new { error = "Invalid Status. Must be 'Ongoing', 'Completed', or 'Canceled'." });
                }

                string posterFolder = $"tvseries/{model.Title}/poster";
                string posterPosterUrl = await _s3Service.UploadFileAsync(model.PosterImageFile, posterFolder);

                string backdropFolder = $"tvseries/{model.Title}/backdrop";
                string backdropPosterUrl = await _s3Service.UploadFileAsync(model.BackdropImageFile, backdropFolder);

                // Tạo TV series mới
                var series = new TvSeries
                {
                    Title = model.Title,
                    Overview = model.Overview,
                    Genres = model.Genres != null ? string.Join(",", model.Genres) : null,
                    Status = model.Status,
                    ReleaseDate = model.ReleaseDate,
                    Studio = model.Studio,
                    Director = model.Director,
                    PosterUrl = posterPosterUrl,
                    BackdropUrl = backdropPosterUrl
                };
                _context.TvSeries.Add(series);
                await _context.SaveChangesAsync();

                // Tự động tạo Season 1 cho TV series
                var season = new Season
                {
                    TvSeriesId = series.Id, // Gán TvSeriesId từ TV series vừa tạo
                    SeasonNumber = 1
                };
                _context.Seasons.Add(season);
                await _context.SaveChangesAsync();

                // Trả về response
                var response = new TvSeriesResponseDTO
                {
                    Id = series.Id,
                    Title = series.Title,
                    Overview = series.Overview,
                    Genres = series.Genres,
                    Status = series.Status,
                    ReleaseDate = series.ReleaseDate,
                    Studio = series.Studio,
                    Director = series.Director,
                    PosterUrl = series.PosterUrl,
                    BackdropUrl = series.BackdropUrl
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Upload failed", details = ex.Message });
            }
        }

        [HttpPost("seasons")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateSeason([FromBody] SeasonDTO model)
        {
            try
            {
                if (model.TvSeriesId <= 0)
                    return BadRequest(new { error = "TvSeriesId is required and must be greater than 0" });
                if (model.SeasonNumber <= 0)
                    return BadRequest(new { error = "SeasonNumber is required and must be greater than 0" });

                var tvSeries = await _context.TvSeries.FindAsync(model.TvSeriesId);
                if (tvSeries == null)
                    return NotFound(new { error = "TV series not found" });

                var existingSeason = await _context.Seasons
                    .FirstOrDefaultAsync(s => s.TvSeriesId == model.TvSeriesId && s.SeasonNumber == model.SeasonNumber);
                if (existingSeason != null)
                    return BadRequest(new { error = $"Season {model.SeasonNumber} for TV series {model.TvSeriesId} already exists" });

                var season = new Season
                {
                    TvSeriesId = model.TvSeriesId,
                    SeasonNumber = model.SeasonNumber
                };
                _context.Seasons.Add(season);
                await _context.SaveChangesAsync();

                var response = new SeasonResponseDTO
                {
                    Id = season.Id,
                    TvSeriesId = season.TvSeriesId,
                    SeasonNumber = season.SeasonNumber
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Creation failed", details = ex.Message });
            }
        }

        [HttpPost("episodes/upload")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UploadEpisode([FromForm] EpisodeUploadDTO model)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (model.SeasonId <= 0)
                    return BadRequest(new { error = "TvSeriesId is required and must be greater than 0" });
                if (model.EpisodeNumber <= 0)
                    return BadRequest(new { error = "EpisodeNumber is required and must be greater than 0" });
                if (model.VideoFile == null)
                    return BadRequest(new { error = "VideoFile is required" });

                // Kiểm tra TV series có tồn tại không
                var tvSeries = await _context.TvSeries.FindAsync(model.TvSeriesId);
                if (tvSeries == null)
                    return NotFound(new { error = "TV series not found" });

                // Kiểm tra season
                Season season;
                if (model.SeasonId > 0)
                {
                    // Nếu có SeasonId, kiểm tra season có tồn tại không
                    season = await _context.Seasons.FindAsync(model.SeasonId);
                    if (season == null)
                        return NotFound(new { error = "Season not found" });
                    if (season.TvSeriesId != model.TvSeriesId)
                        return BadRequest(new { error = "Season does not belong to the specified TV series" });
                }
                else
                {
                    // Nếu không có SeasonId, kiểm tra xem TV series đã có season nào chưa
                    season = await _context.Seasons
                        .Where(s => s.TvSeriesId == model.TvSeriesId)
                        .OrderBy(s => s.SeasonNumber)
                        .FirstOrDefaultAsync();

                    if (season == null)
                    {
                        // Nếu chưa có season, tạo Season 1
                        season = new Season
                        {
                            TvSeriesId = model.TvSeriesId,
                            SeasonNumber = 1
                        };
                        _context.Seasons.Add(season);
                        await _context.SaveChangesAsync();
                    }
                }

                // Kiểm tra episode đã tồn tại chưa
                var existingEpisode = await _context.Episodes
                    .FirstOrDefaultAsync(e => e.SeasonId == season.Id && e.EpisodeNumber == model.EpisodeNumber);
                if (existingEpisode != null)
                    return BadRequest(new { error = $"Episode {model.EpisodeNumber} for Season {season.Id} already exists" });

                // Upload video lên S3
                string videoFolder = $"tvseries/{season.TvSeriesId}/season-{season.SeasonNumber}/episode-{model.EpisodeNumber}";
                string videoUrl = await _s3Service.UploadFileAsync(model.VideoFile, videoFolder);

                // Tạo episode mới
                var episode = new Episode
                {
                    SeasonId = season.Id,
                    EpisodeNumber = model.EpisodeNumber,
                    VideoUrl = videoUrl
                };
                _context.Episodes.Add(episode);
                await _context.SaveChangesAsync();

                // Trả về response
                var response = new EpisodeResponseDTO
                {
                    Id = episode.Id,
                    SeasonId = episode.SeasonId,
                    EpisodeNumber = episode.EpisodeNumber,
                    VideoUrl = episode.VideoUrl
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Upload failed", details = ex.Message });
            }
        }

        // [HttpPut("{id}")]
        // [Authorize(Roles = "admin")]
        // public async Task<IActionResult> UpdateTvSeries(int id, [FromBody] TvSeriesDTO updatedSeriesDto)
        // {
        //     var series = _context.TvSeries.Find(id);
        //     if (series == null) return NotFound();

        //     var validStatuses = new[] { "Ongoing", "Completed", "Canceled" };
        //     if (!validStatuses.Contains(updatedSeriesDto.Status))
        //     {
        //         return BadRequest(new { error = "Invalid Status. Must be 'Ongoing', 'Completed', or 'Canceled'." });
        //     }

        //     series.Title = updatedSeriesDto.Title;
        //     series.Overview = updatedSeriesDto.Overview;
        //     series.Genres = updatedSeriesDto.Genres;
        //     series.Status = updatedSeriesDto.Status;
        //     series.ReleaseDate = updatedSeriesDto.ReleaseDate;
        //     series.Studio = updatedSeriesDto.Studio;
        //     series.Director = updatedSeriesDto.Director;

        //     await _context.SaveChangesAsync();
        //     return NoContent();
        // }

        [HttpGet("{id}/seasons")]
        public IActionResult GetSeasonsByTvSeries(int id)
        {
            var tvSeries = _context.TvSeries.Find(id);
            if (tvSeries == null) return NotFound(new { error = "TV series not found" });

            var seasons = _context.Seasons
                .Where(s => s.TvSeriesId == id)
                .Select(s => new SeasonResponseDTO
                {
                    Id = s.Id,
                    TvSeriesId = s.TvSeriesId,
                    SeasonNumber = s.SeasonNumber
                })
                .ToList();

            return Ok(seasons);
        }

        [HttpGet("seasons/{seasonId}/episodes")]
        public IActionResult GetEpisodesBySeason(int seasonId)
        {
            // Kiểm tra season có tồn tại không
            var season = _context.Seasons.Find(seasonId);
            if (season == null)
                return NotFound(new { error = "Season not found" });

            // Lấy danh sách episodes của season
            var episodes = _context.Episodes
                .Where(e => e.SeasonId == seasonId)
                .Select(e => new EpisodeResponseDTO
                {
                    Id = e.Id,
                    SeasonId = e.SeasonId,
                    EpisodeNumber = e.EpisodeNumber,
                    VideoUrl = e.VideoUrl
                })
                .ToList();

            return Ok(episodes);
        }

        [HttpGet("episodes/{episodeId}")]
        public IActionResult GetEpisode(int episodeId)
        {
            var episode = _context.Episodes
                .Where(e => e.Id == episodeId)
                .Select(e => new EpisodeResponseDTO
                {
                    Id = e.Id,
                    SeasonId = e.SeasonId,
                    EpisodeNumber = e.EpisodeNumber,
                    VideoUrl = e.VideoUrl
                })
                .FirstOrDefault();

            if (episode == null)
            {
                return NotFound(new { error = "Episode not found" });
            }

            return Ok(episode);
        }

        [HttpGet("most-viewed")]
        public async Task<IActionResult> GetMostViewedTvSeries()
        {
            var mostViewedTvSeries = await _context.TvSeries
                .OrderByDescending(t => t.ViewCount)
                .Take(20)
                .ToListAsync();

            return Ok(mostViewedTvSeries);
        }

        // [HttpDelete("{id}")]
        // [Authorize(Roles = "admin")]
        // public async Task<IActionResult> DeleteTvSeries(int id)
        // {
        //     var series = _context.TvSeries.Find(id);
        //     if (series == null) return NotFound();

        //     try
        //     {
        //         if (!string.IsNullOrEmpty(series.PosterUrl))
        //         {
        //             await _s3Service.DeleteFileAsync(series.PosterUrl);
        //         }
        //         if (!string.IsNullOrEmpty(series.BackdropUrl))
        //         {
        //             await _s3Service.DeleteFileAsync(series.BackdropUrl);
        //         }

        //         _context.TvSeries.Remove(series);
        //         await _context.SaveChangesAsync();
        //         return NoContent();
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, new { error = "Failed to delete TV series", details = ex.Message });
        //     }
        // }
    }
}