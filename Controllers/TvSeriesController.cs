using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using backend.Models;
using backend.Data;

namespace backend.Controllers
{
    [Route("api/tvseries")]
    [ApiController]
    public class TvSeriesController : ControllerBase
    {
        private readonly MovieDbContext _context;

        public TvSeriesController(MovieDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAllTvSeries()
        {
            var series = _context.TvSeries.ToList();
            return Ok(series);
        }

        [HttpGet("{id}")]
        public IActionResult GetTvSeries(int id)
        {
            var series = _context.TvSeries.Find(id);
            if (series == null) return NotFound();
            return Ok(series);
        }

        [HttpPost]
        public IActionResult CreateTvSeries([FromBody] TvSeries series)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.TvSeries.Add(series);
            _context.SaveChanges();
            return CreatedAtAction(nameof(GetTvSeries), new { id = series.Id }, series);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateTvSeries(int id, [FromBody] TvSeries updatedSeries)
        {
            var series = _context.TvSeries.Find(id);
            if (series == null) return NotFound();

            series.Title = updatedSeries.Title;
            series.Overview = updatedSeries.Overview;
            series.Genres = updatedSeries.Genres;
            series.Status = updatedSeries.Status;
            series.ReleaseDate = updatedSeries.ReleaseDate;
            series.Studio = updatedSeries.Studio;
            series.Director = updatedSeries.Director;
            series.ImageUrl = updatedSeries.ImageUrl;

            _context.SaveChanges();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteTvSeries(int id)
        {
            var series = _context.TvSeries.Find(id);
            if (series == null) return NotFound();

            _context.TvSeries.Remove(series);
            _context.SaveChanges();
            return NoContent();
        }
    }
}
