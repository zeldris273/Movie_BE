using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using backend.Models;


namespace backend.Data
{
    public class MovieDbContext : DbContext
    {
        public MovieDbContext(DbContextOptions<MovieDbContext> options) : base(options) { }

        public DbSet<Movie> Movies { get; set; }
        public DbSet<Actor> Actors { get; set; }
        public DbSet<MovieCast> MovieCasts { get; set; }
        public DbSet<TvSeries> TvSeries { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Movie>()
                .ToTable("Movies");

            // Ánh xạ thuộc tính ImageUrls với cột image_url
            modelBuilder.Entity<Movie>()
                .Property(m => m.ImageUrls)
                .HasColumnName("image_url");

            // Ánh xạ các cột khác nếu cần
            modelBuilder.Entity<Movie>()
                .Property(m => m.VideoUrl)
                .HasColumnName("video_url");

            modelBuilder.Entity<Movie>()
                .Property(m => m.ReleaseDate)
                .HasColumnName("release_date");
        }
    }
}