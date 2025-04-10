using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data
{
    public class MovieDbContext : DbContext
    {
        public MovieDbContext(DbContextOptions<MovieDbContext> options) : base(options) { }

        public DbSet<Movie> Movies { get; set; }
        public DbSet<TvSeries> TvSeries { get; set; }
        public DbSet<Season> Seasons { get; set; }
        public DbSet<Episode> Episodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình ánh xạ cho Movie
            modelBuilder.Entity<Movie>(entity =>
            {
                entity.ToTable("movies");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.Overview).HasColumnName("overview");
                entity.Property(e => e.Genres).HasColumnName("genres");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.ReleaseDate).HasColumnName("release_date");
                entity.Property(e => e.Studio).HasColumnName("studio");
                entity.Property(e => e.Director).HasColumnName("director");
                entity.Property(e => e.ImageUrl).HasColumnName("image_url");
                entity.Property(e => e.VideoUrl).HasColumnName("video_url");
            });

            // Cấu hình mối quan hệ cho TvSeries, Season, Episode
           modelBuilder.Entity<Season>(entity =>
            {
                entity.ToTable("seasons");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TvSeriesId).HasColumnName("TvSeriesId");
                entity.Property(e => e.SeasonNumber).HasColumnName("SeasonNumber");

                // Cấu hình quan hệ giữa Season và TvSeries
                entity.HasOne(s => s.TvSeries)
                      .WithMany(t => t.Seasons)
                      .HasForeignKey(s => s.TvSeriesId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình ánh xạ cho Episode
            modelBuilder.Entity<Episode>(entity =>
            {
                entity.ToTable("episodes");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.SeasonId).HasColumnName("season_id");
                entity.Property(e => e.EpisodeNumber).HasColumnName("episode_number");
                entity.Property(e => e.VideoUrl).HasColumnName("video_url");

                // Cấu hình quan hệ giữa Episode và Season
                entity.HasOne(e => e.Season)
                      .WithMany(s => s.Episodes)
                      .HasForeignKey(e => e.SeasonId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}