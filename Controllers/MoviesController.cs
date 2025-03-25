using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.DTOs;
using backend.Mappers;

[Route("api/movies")]
[ApiController]
public class MoviesController : ControllerBase
{
    private readonly MovieDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public MoviesController(MovieDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadMovie([FromForm] MovieDTO movieDto)
    {
        if (movieDto.VideoFile == null || movieDto.ImageFiles == null || movieDto.ImageFiles.Count != 2)
    {
        return BadRequest("Phải có 1 video và 2 ảnh.");
    }

        // Thư mục lưu trữ
        var basePath = _environment.WebRootPath ?? _environment.ContentRootPath;
        var uploadPath = Path.Combine(basePath, "uploads");
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        // Lưu video
        var videoFileName = $"{Guid.NewGuid()}_{movieDto.VideoFile.FileName}";
        var videoPath = Path.Combine(uploadPath, videoFileName);
        using (var stream = new FileStream(videoPath, FileMode.Create))
        {
            await movieDto.VideoFile.CopyToAsync(stream);
        }

        // Lưu ảnh
        var imageUrls = new List<string>();
        foreach (var image in movieDto.ImageFiles)
        {
            var imageFileName = $"{Guid.NewGuid()}_{image.FileName}";
            var imagePath = Path.Combine(uploadPath, imageFileName);
            using (var stream = new FileStream(imagePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }
            imageUrls.Add($"/uploads/{imageFileName}");
        }

        // Tạo đối tượng Movie
       var newMovie = MovieMapper.ToMovie(movieDto, $"/uploads/{videoFileName}", imageUrls);

        _context.Movies.Add(newMovie);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Upload thành công!",
            videoUrl = newMovie.VideoUrl,
            imageUrls = imageUrls
        });
    }
}
