using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using backend.Models;
using backend.Data;
using backend.DTOs;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Movie_BE.DTOs;
using Movie_BE.Models;

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
                    Rating = (double?)s.Rating,
                    ReleaseDate = s.ReleaseDate,
                    PosterUrl = s.PosterUrl,
                    BackdropUrl = s.BackdropUrl
                })
                .ToList();
            return Ok(series);
        }

        [HttpGet("{id}/{title}")]
        public IActionResult GetTvSeries(int id, string title)
        {
            var series = _context.TvSeries
                .Include(s => s.TvSeriesActors) // Tải TvSeriesActors
                .ThenInclude(ta => ta.Actor)    // Tải Actor liên quan
                .FirstOrDefault(s => s.Id == id);

            if (series == null) return NotFound(new { error = "TV series not found" });

            series.ViewCount = (series.ViewCount ?? 0) + 1;
            _context.SaveChanges();

            string expectedSlug = series.Title.ToLower()
                .Replace(" ", "-")
                .Trim();
            expectedSlug = Regex.Replace(expectedSlug, "[^a-z0-9-]", "");
            expectedSlug = Regex.Replace(expectedSlug, "-+", "-");

            Console.WriteLine($"Title: {title}");
            Console.WriteLine($"Expected slug: {expectedSlug}");
            if (title != expectedSlug)
            {
                return NotFound(new { error = "Invalid Film" });
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
                TrailerUrl = series.TrailerUrl,
                Actors = series.TvSeriesActors.Select(ta => new ActorDTO
                {
                    Id = ta.Actor.Id,
                    Name = ta.Actor.Name
                }).ToList()
            };
            return Ok(response);
        }

        [HttpGet("{id}/{title}/episode/{episodeNumber}/watch")]
        public async Task<IActionResult> WatchTvSeriesEpisode(int id, string title, int episodeNumber)
        {
            var series = _context.TvSeries.Find(id);
            if (series == null) return NotFound(new { error = "TV series not found" });

            string expectedSeriesSlug = series.Title.ToLower()
                .Replace(" ", "-")
                .Trim();
            expectedSeriesSlug = Regex.Replace(expectedSeriesSlug, "[^a-z0-9-]", "");
            expectedSeriesSlug = Regex.Replace(expectedSeriesSlug, "-+", "-");

            if (title != expectedSeriesSlug)
            {
                return NotFound(new { error = "TV series not found" });
            }

            var episode = _context.Episodes
                .Where(e => e.Season.TvSeriesId == id && e.EpisodeNumber == episodeNumber)
                .FirstOrDefault();

            if (episode == null) return NotFound(new { error = "Episode not found" });

            var season = _context.Seasons.Find(episode.SeasonId);
            if (season == null || season.TvSeriesId != id)
                return BadRequest(new { error = "Episode does not belong to this TV series" });

            if (string.IsNullOrEmpty(episode.VideoUrl))
                return BadRequest(new { error = "Video URL not available for this episode" });

            series.ViewCount = (series.ViewCount ?? 0) + 1;
            await _context.SaveChangesAsync();

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

                // Xử lý diễn viên
                List<string> actors = string.IsNullOrEmpty(model.Actors)
                    ? new List<string>()
                    : model.Actors.Split(',').Select(actor => actor.Trim()).ToList();

                foreach (var actorName in actors)
                {
                    if (!string.IsNullOrWhiteSpace(actorName))
                    {
                        var actor = await _context.Actors
                            .FirstOrDefaultAsync(a => a.Name.ToLower() == actorName.ToLower());

                        if (actor == null)
                        {
                            actor = new Actor
                            {
                                Name = actorName,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.Actors.Add(actor);
                            await _context.SaveChangesAsync();
                        }

                        var tvSeriesActor = new TvSeriesActor
                        {
                            TvSeriesId = series.Id,
                            ActorId = actor.Id,
                            CharacterName = "" // Có thể thêm trường để nhập tên nhân vật
                        };
                        _context.Set<TvSeriesActor>().Add(tvSeriesActor);
                    }
                }

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
                    BackdropUrl = series.BackdropUrl,
                    Actors = series.TvSeriesActors.Select(ta => new ActorDTO
                    {
                        Id = ta.Actor.Id,
                        Name = ta.Actor.Name
                    }).ToList()
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
                if (model.HlsZipFile == null)
                    return BadRequest(new { error = "HlsZipFile is required" });

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

                // Tạo thư mục tạm để giải nén file .zip
                var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                try
                {
                    // Lưu file .zip tạm thời
                    var zipPath = Path.Combine(tempDir, model.HlsZipFile.FileName);
                    using (var stream = new FileStream(zipPath, FileMode.Create))
                    {
                        await model.HlsZipFile.CopyToAsync(stream);
                    }

                    // Giải nén file .zip
                    var extractPath = Path.Combine(tempDir, "extracted");
                    ZipFile.ExtractToDirectory(zipPath, extractPath);

                    // Upload thư mục HLS lên S3
                    string videoFolder = $"tvseries/{tvSeries.Title}/season-{season.SeasonNumber}/episode-{model.EpisodeNumber}";
                    string videoUrl = await _s3Service.UploadHlsFolderAsync(extractPath, videoFolder);

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
                finally
                {
                    // Xóa thư mục tạm
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
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

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateTvSeries(int id, [FromBody] TvSeriesResponseDTO model)
        {
            try
            {
                var series = await _context.TvSeries
                    .Include(s => s.TvSeriesActors)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (series == null)
                    return NotFound(new { error = "TV series not found" });

                // Cập nhật các trường cơ bản
                series.Title = model.Title ?? series.Title;
                series.Overview = model.Overview ?? series.Overview;
                series.Genres = model.Genres ?? series.Genres;
                series.Status = model.Status ?? series.Status;
                series.ReleaseDate = model.ReleaseDate ?? series.ReleaseDate;
                series.Studio = model.Studio ?? series.Studio;
                series.Director = model.Director ?? series.Director;
                series.PosterUrl = model.PosterUrl ?? series.PosterUrl;
                series.BackdropUrl = model.BackdropUrl ?? series.BackdropUrl;
                series.TrailerUrl = model.TrailerUrl ?? series.TrailerUrl;

                // Kiểm tra Status hợp lệ
                var validStatuses = new[] { "Ongoing", "Completed", "Canceled" };
                if (!string.IsNullOrEmpty(model.Status) && !validStatuses.Contains(model.Status))
                {
                    return BadRequest(new { error = "Invalid Status. Must be 'Ongoing', 'Completed', or 'Canceled'." });
                }

                // Xử lý diễn viên (xóa các liên kết cũ và thêm mới)
                if (model.Actors != null && model.Actors.Any())
                {
                    // Xóa các liên kết TvSeriesActor cũ
                    _context.Set<TvSeriesActor>().RemoveRange(series.TvSeriesActors);

                    // Thêm các diễn viên mới
                    foreach (var actorDto in model.Actors)
                    {
                        var actor = await _context.Actors
                            .FirstOrDefaultAsync(a => a.Name.ToLower() == actorDto.Name.ToLower());

                        if (actor == null)
                        {
                            actor = new Actor
                            {
                                Name = actorDto.Name,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.Actors.Add(actor);
                            await _context.SaveChangesAsync();
                        }

                        var tvSeriesActor = new TvSeriesActor
                        {
                            TvSeriesId = series.Id,
                            ActorId = actor.Id,
                            CharacterName = ""
                        };
                        _context.Set<TvSeriesActor>().Add(tvSeriesActor);
                    }
                }

                await _context.SaveChangesAsync();

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
                    TrailerUrl = series.TrailerUrl,
                    Actors = series.TvSeriesActors.Select(ta => new ActorDTO
                    {
                        Id = ta.Actor.Id,
                        Name = ta.Actor.Name
                    }).ToList()
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Update failed", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteTvSeries(int id)
        {
            try
            {
                var series = await _context.TvSeries
                    .Include(s => s.TvSeriesActors)
                    .Include(s => s.Seasons)
                    .ThenInclude(s => s.Episodes)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (series == null)
                    return NotFound(new { error = "TV series not found" });

                // Xóa các liên kết TvSeriesActor
                _context.Set<TvSeriesActor>().RemoveRange(series.TvSeriesActors);

                // Xóa các episode trong season
                foreach (var season in series.Seasons)
                {
                    var episodes = _context.Episodes.Where(e => e.SeasonId == season.Id);
                    _context.Episodes.RemoveRange(episodes);
                }

                // Xóa các season
                _context.Seasons.RemoveRange(series.Seasons);

                // Xóa TV series
                _context.TvSeries.Remove(series);

                // Xóa các file liên quan trên S3 (poster, backdrop, episodes)
                // if (!string.IsNullOrEmpty(series.PosterUrl))
                //     await _s3Service.DeleteFileAsync(series.PosterUrl);
                // if (!string.IsNullOrEmpty(series.BackdropUrl))
                //     await _s3Service.DeleteFileAsync(series.BackdropUrl);

                // foreach (var season in series.Seasons)
                // {
                //     var episodes = _context.Episodes.Where(e => e.SeasonId == season.Id);
                //     foreach (var episode in episodes)
                //     {
                //         if (!string.IsNullOrEmpty(episode.VideoUrl))
                //             await _s3Service.DeleteFileAsync(episode.VideoUrl);
                //     }
                // }

                await _context.SaveChangesAsync();
                return Ok(new { message = "TV series deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Delete failed", details = ex.Message });
            }
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