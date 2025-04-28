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
                    NumberOfRatings = s.NumberOfRatings,
                    Genres = s.Genres,
                    Status = s.Status,
                    ReleaseDate = s.ReleaseDate,
                    Studio = s.Studio,
                    Director = s.Director,
                    PosterUrl = s.PosterUrl,
                    BackdropUrl = s.BackdropUrl,
                    TrailerUrl = s.TrailerUrl
                })
                .ToList();
            return Ok(series);
        }

        // Endpoint cho URL xem chi tiết: /api/tvseries/{id}/{title}
        [HttpGet("{id}/{title}")]
        public IActionResult GetTvSeries(int id, string title)
        {
            var series = _context.TvSeries.Find(id);
            if (series == null) return NotFound(new { error = "TV series not found" });

            // Tăng ViewCount khi truy cập chi tiết
            series.ViewCount = (series.ViewCount ?? 0) + 1;
            _context.SaveChanges();

            // Kiểm tra slug (title) để SEO
            string expectedSlug = series.Title.ToLower()
                .Replace(" ", "-")
                .Replace("[^a-z0-9-]", "");
            if (title != expectedSlug)
            {
                // return NotFound(new { error = "Invalid Film" });
            }

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

        // Endpoint cho URL xem phim: /api/tvseries/{id}/{episodeId}/watch
        [HttpGet("{id}/{episodeId}/watch")]
        public IActionResult WatchTvSeriesEpisode(int id, int episodeId)
        {
            var series = _context.TvSeries.Find(id);
            if (series == null) return NotFound(new { error = "TV series not found" });

            var episode = _context.Episodes
                .Where(e => e.Id == episodeId)
                .FirstOrDefault();

            if (episode == null) return NotFound(new { error = "Episode not found" });

            // Kiểm tra xem episode có thuộc TV series không
            var season = _context.Seasons.Find(episode.SeasonId);
            if (season == null || season.TvSeriesId != id)
                return BadRequest(new { error = "Episode does not belong to this TV series" });

            if (string.IsNullOrEmpty(episode.VideoUrl))
                return BadRequest(new { error = "Video URL not available for this episode" });

            return Ok(new { videoUrl = episode.VideoUrl });
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

                var season = new Season
                {
                    TvSeriesId = series.Id,
                    SeasonNumber = 1
                };
                _context.Seasons.Add(season);
                await _context.SaveChangesAsync();

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
                if (model.SeasonId <= 0)
                    return BadRequest(new { error = "SeasonId is required and must be greater than 0" });
                if (model.EpisodeNumber <= 0)
                    return BadRequest(new { error = "EpisodeNumber is required and must be greater than 0" });
                if (model.VideoFile == null)
                    return BadRequest(new { error = "VideoFile is required" });

                var tvSeries = await _context.TvSeries.FindAsync(model.TvSeriesId);
                if (tvSeries == null)
                    return NotFound(new { error = "TV series not found" });

                Season season;
                if (model.SeasonId > 0)
                {
                    season = await _context.Seasons.FindAsync(model.SeasonId);
                    if (season == null)
                        return NotFound(new { error = "Season not found" });
                    if (season.TvSeriesId != model.TvSeriesId)
                        return BadRequest(new { error = "Season does not belong to the specified TV series" });
                }
                else
                {
                    season = await _context.Seasons
                        .Where(s => s.TvSeriesId == model.TvSeriesId)
                        .OrderBy(s => s.SeasonNumber)
                        .FirstOrDefaultAsync();

                    if (season == null)
                    {
                        season = new Season
                        {
                            TvSeriesId = model.TvSeriesId,
                            SeasonNumber = 1
                        };
                        _context.Seasons.Add(season);
                        await _context.SaveChangesAsync();
                    }
                }

                var existingEpisode = await _context.Episodes
                    .FirstOrDefaultAsync(e => e.SeasonId == season.Id && e.EpisodeNumber == model.EpisodeNumber);
                if (existingEpisode != null)
                    return BadRequest(new { error = $"Episode {model.EpisodeNumber} for Season {season.Id} already exists" });

                string videoFolder = $"tvseries/{season.TvSeriesId}/season-{season.SeasonNumber}/episode-{model.EpisodeNumber}";
                string videoUrl = await _s3Service.UploadFileAsync(model.VideoFile, videoFolder);

                var episode = new Episode
                {
                    SeasonId = season.Id,
                    EpisodeNumber = model.EpisodeNumber,
                    VideoUrl = videoUrl
                };
                _context.Episodes.Add(episode);
                await _context.SaveChangesAsync();

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
            var season = _context.Seasons.Find(seasonId);
            if (season == null)
                return NotFound(new { error = "Season not found" });

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
    }
}