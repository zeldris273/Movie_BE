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

        // [HttpGet]
        // public IActionResult GetAllTvSeries()
        // {
        //     var series = _context.TvSeries
        //         .Select(s => new TvSeriesResponseDTO
        //         {
        //             Id = s.Id,
        //             Title = s.Title,
        //             Overview = s.Overview,
        //             Genres = s.Genres,
        //             Status = s.Status,
        //             ReleaseDate = s.ReleaseDate,
        //             Studio = s.Studio,
        //             Director = s.Director,
        //             ImageUrl = s.ImageUrl,
        //             BackdropUrl = s.BackdropUrl
        //         })
        //         .ToList();
        //     return Ok(series);
        // }

        // [HttpGet("{id}")]
        // public IActionResult GetTvSeries(int id)
        // {
        //     var series = _context.TvSeries.Find(id);
        //     if (series == null) return NotFound();

        //     var response = new TvSeriesResponseDTO
        //     {
        //         Id = series.Id,
        //         Title = series.Title,
        //         Overview = series.Overview,
        //         Genres = s.Genres,
        //         Status = series.Status,
        //         ReleaseDate = series.ReleaseDate,
        //         Studio = series.Studio,
        //         Director = series.Director,
        //         ImageUrl = series.ImageUrl,
        //         BackdropUrl = series.BackdropUrl
        //     };
        //     return Ok(response);
        // }

        [HttpPost("upload")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UploadTvSeries([FromForm] TvSeriesUploadDTO model)
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
                string posterImageUrl = await _s3Service.UploadFileAsync(model.PosterImageFile, posterFolder);

                string backdropFolder = $"tvseries/{model.Title}/backdrop";
                string backdropImageUrl = await _s3Service.UploadFileAsync(model.BackdropImageFile, backdropFolder);

                var series = new TvSeries
                {
                    Title = model.Title,
                    Overview = model.Overview,
                    Genres = model.Genres != null ? string.Join(",", model.Genres) : null,
                    Status = model.Status,
                    ReleaseDate = model.ReleaseDate,
                    Studio = model.Studio,
                    Director = model.Director,
                    ImageUrl = posterImageUrl,
                    BackdropUrl = backdropImageUrl
                };
                _context.TvSeries.Add(series);
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
                    ImageUrl = series.ImageUrl,
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

                var season = await _context.Seasons.FindAsync(model.SeasonId);
                if (season == null)
                    return NotFound(new { error = "Season not found" });

                var existingEpisode = await _context.Episodes
                    .FirstOrDefaultAsync(e => e.SeasonId == model.SeasonId && e.EpisodeNumber == model.EpisodeNumber);
                if (existingEpisode != null)
                    return BadRequest(new { error = $"Episode {model.EpisodeNumber} for Season {model.SeasonId} already exists" });

                string videoFolder = $"tvseries/{season.TvSeriesId}/season-{season.SeasonNumber}/episode-{model.EpisodeNumber}";
                string videoUrl = await _s3Service.UploadFileAsync(model.VideoFile, videoFolder);

                var episode = new Episode
                {
                    SeasonId = model.SeasonId,
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

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateTvSeries(int id, [FromBody] TvSeriesDTO updatedSeriesDto)
        {
            var series = _context.TvSeries.Find(id);
            if (series == null) return NotFound();

            var validStatuses = new[] { "Ongoing", "Completed", "Canceled" };
            if (!validStatuses.Contains(updatedSeriesDto.Status))
            {
                return BadRequest(new { error = "Invalid Status. Must be 'Ongoing', 'Completed', or 'Canceled'." });
            }

            series.Title = updatedSeriesDto.Title;
            series.Overview = updatedSeriesDto.Overview;
            series.Genres = updatedSeriesDto.Genres;
            series.Status = updatedSeriesDto.Status;
            series.ReleaseDate = updatedSeriesDto.ReleaseDate;
            series.Studio = updatedSeriesDto.Studio;
            series.Director = updatedSeriesDto.Director;

            await _context.SaveChangesAsync();
            return NoContent();
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

        // [HttpDelete("{id}")]
        // [Authorize(Roles = "admin")]
        // public async Task<IActionResult> DeleteTvSeries(int id)
        // {
        //     var series = _context.TvSeries.Find(id);
        //     if (series == null) return NotFound();

        //     try
        //     {
        //         if (!string.IsNullOrEmpty(series.ImageUrl))
        //         {
        //             await _s3Service.DeleteFileAsync(series.ImageUrl);
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