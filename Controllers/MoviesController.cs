using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.DTOs;
using backend.Mappers;
using backend.Services;

[Route("api/movies")]
[ApiController]
public class MoviesController : ControllerBase
{
    private readonly MovieDbContext _context;
    private readonly S3Service _s3Service;

    public MoviesController(MovieDbContext context, S3Service s3Service)
    {
        _context = context;
        _s3Service = s3Service;
    }

    [HttpPost("upload")]
    [RequestFormLimits(MultipartBodyLengthLimit = 1024 * 1024 * 500)] // 500MB
    [RequestSizeLimit(1024 * 1024 * 500)] // 500MB
    public async Task<IActionResult> UploadMovie([FromForm] MovieDTO movieDto)
    {
        try
        {
            // Kiểm tra dữ liệu đầu vào
            if (movieDto == null)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            if (string.IsNullOrEmpty(movieDto.Title) || string.IsNullOrEmpty(movieDto.Status) || string.IsNullOrEmpty(movieDto.Type))
            {
                return BadRequest("Title, Status và Type là bắt buộc.");
            }

            if (movieDto.VideoFile == null || movieDto.ImageFiles == null || movieDto.ImageFiles.Count != 2)
            {
                return BadRequest("Phải có 1 video và đúng 2 ảnh.");
            }

            // Upload video lên S3
            var videoUrl = await _s3Service.UploadFileAsync(movieDto.VideoFile, "videos");

            // Upload ảnh lên S3
            var imageUrls = new List<string>();
            foreach (var image in movieDto.ImageFiles)
            {
                var imageUrl = await _s3Service.UploadFileAsync(image, "images");
                imageUrls.Add(imageUrl);
            }

            // Tạo đối tượng Movie
            var newMovie = MovieMapper.ToMovie(movieDto, videoUrl, imageUrls);

            // Lưu vào database
            _context.Movies.Add(newMovie);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Upload thành công!",
                videoUrl = newMovie.VideoUrl,
                imageUrls = imageUrls
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Lỗi upload",
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
}