using System;
using System.IO;
using System.Threading.Tasks;
using backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController : ControllerBase
    {
        private readonly S3Service _s3Service;

        public MoviesController(S3Service s3Service)
        {
            _s3Service = s3Service;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadMovie(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var allowedExtensions = new[] { ".mp4", ".mkv", ".avi" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                return BadRequest("Invalid file format. Only MP4, MKV, AVI are allowed.");

            if (file.Length > 500 * 1024 * 1024)
                return BadRequest("File size exceeds 500MB.");

            try
            {
                await using var stream = file.OpenReadStream();
                string fileUrl = await _s3Service.UploadVideoAsync(stream, file.FileName, file.ContentType);
                return Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error uploading file: {ex.Message}");
            }
        }
    }
}