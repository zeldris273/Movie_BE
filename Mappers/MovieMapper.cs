using backend.Models;
using backend.DTOs;
using System.Collections.Generic;

namespace backend.Mappers
{
    public static class MovieMapper
    {
        public static Movie ToMovie(MovieDTO movieDto, string videoUrl, List<string> imageUrls)
        {
            return new Movie
            {
                Title = movieDto.Title,
                Rating = movieDto.Rating,
                Overview = movieDto.Overview,
                Genres = string.Join(",", movieDto.Genres),
                Status = movieDto.Status,
                ReleaseDate = movieDto.ReleaseDate,
                Type = movieDto.Type,
                Studio = movieDto.Studio,
                Director = movieDto.Director,
                VideoUrl = videoUrl,
                ImageUrls = string.Join(",", imageUrls)
            };
        }
    }
}