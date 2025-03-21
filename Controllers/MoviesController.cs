using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public async Task<IActionResult> UploadMovie(
     [FromForm] IFormFile videoFile,
     [FromForm] List<IFormFile> imageFiles)
        {
            if (videoFile == null || videoFile.Length == 0)
                return BadRequest("No video file uploaded");

            if (imageFiles == null || imageFiles.Count == 0)
                return BadRequest("No image files uploaded");

            var allowedVideoExtensions = new[] { ".mp4", ".avi", ".mov" };
            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png" };

            var videoExtension = Path.GetExtension(videoFile.FileName).ToLower();
            if (!allowedVideoExtensions.Contains(videoExtension))
                return BadRequest("Invalid video format. Only MP4, AVI, MOV are allowed.");

            if (videoFile.Length > 500 * 1024 * 1024) // 500MB
                return BadRequest("Video file size exceeds the maximum limit of 500MB.");

            var videoUrl = await _s3Service.UploadFileAsync(
                videoFile.OpenReadStream(),
                videoFile.FileName,
                "videos",
                videoFile.ContentType
            );

            var imageUrls = new List<string>();
            foreach (var imageFile in imageFiles)
            {
                var imageExtension = Path.GetExtension(imageFile.FileName).ToLower();
                if (!allowedImageExtensions.Contains(imageExtension))
                    return BadRequest($"Invalid image format: {imageFile.FileName}. Only JPG, JPEG, PNG are allowed.");

                var imageUrl = await _s3Service.UploadFileAsync(
                    imageFile.OpenReadStream(),
                    imageFile.FileName,
                    "images",
                    imageFile.ContentType
                );
                imageUrls.Add(imageUrl);
            }

            return Ok(new { VideoUrl = videoUrl, ImageUrls = imageUrls });
        }

    }
}
