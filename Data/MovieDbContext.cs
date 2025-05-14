using Microsoft.EntityFrameworkCore;
using backend.Models;
using Movie_BE.Models;

namespace backend.Data
{
    public class MovieDbContext : DbContext
    {
        public MovieDbContext(DbContextOptions<MovieDbContext> options)
            : base(options) { }

        public DbSet<Movie> Movies { get; set; }
        public DbSet<TvSeries> TvSeries { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Season> Seasons { get; set; }
        public DbSet<Episode> Episodes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<WatchList> WatchList { get; set; }
        public DbSet<Actor> Actors { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<MovieActor> MovieActors { get; set; }
        public DbSet<TvSeriesActor> TvSeriesActors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TvSeries>(entity =>
            {
                entity.ToTable("TvSeries");
                entity.Property(e => e.NumberOfRatings)
                          .HasColumnName("number_of_ratings");
                entity.Property(e => e.ViewCount)
                     .HasColumnName("view_count");
            });
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
                entity.Property(e => e.PosterUrl).HasColumnName("poster_url");
                entity.Property(e => e.BackdropUrl).HasColumnName("backdrop_url");
                entity.Property(e => e.VideoUrl).HasColumnName("video_url");
                entity.Property(e => e.ViewCount).HasColumnName("view_count");
                entity.Property(e => e.NumberOfRatings)
                          .HasColumnName("number_of_ratings");
            });

            // Cấu hình mối quan hệ cho TvSeries, Season, Episode
            modelBuilder.Entity<Season>(entity =>
            {
                entity.ToTable("seasons");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TvSeriesId).HasColumnName("TvSeriesId");
                entity.Property(e => e.SeasonNumber).HasColumnName("SeasonNumber");

                entity.HasOne(s => s.TvSeries)
                          .WithMany(t => t.Seasons)
                          .HasForeignKey(s => s.TvSeriesId)
                          .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Episode>(entity =>
            {
                entity.ToTable("episodes");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.SeasonId).HasColumnName("season_id");
                entity.Property(e => e.EpisodeNumber).HasColumnName("episode_number");
                entity.Property(e => e.VideoUrl).HasColumnName("video_url");

                entity.HasOne(e => e.Season)
                          .WithMany(s => s.Episodes)
                          .HasForeignKey(e => e.SeasonId)
                          .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình ánh xạ cho Comment
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.ToTable("comments");

                entity.HasKey(c => c.Id);

                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.Property(e => e.MovieId).HasColumnName("movie_id");

                entity.Property(e => e.TvSeriesId).HasColumnName("tvseries_id");

                entity.Property(e => e.EpisodeId).HasColumnName("episode_id");

                entity.Property(e => e.ParentCommentId)
                          .HasColumnName("parent_comment_id");

                entity.Property(e => e.CommentText).HasColumnName("comment_text");

                entity.Property(e => e.Timestamp).HasColumnName("timestamp");

                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(c => c.User)
                          .WithMany(u => u.Comments)
                          .HasForeignKey(c => c.UserId)
                          .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Movie)
                          .WithMany(m => m.Comments)
                          .HasForeignKey(c => c.MovieId)
                          .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.TvSeries)
                          .WithMany(t => t.Comments)
                          .HasForeignKey(c => c.TvSeriesId)
                          .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Episode)
                          .WithMany(e => e.Comments)
                          .HasForeignKey(c => c.EpisodeId)
                          .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.ParentComment)
                          .WithMany(c => c.Replies)
                          .HasForeignKey(c => c.ParentCommentId)
                          .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình ánh xạ cho WatchList
            modelBuilder.Entity<WatchList>(entity =>
            {
                entity.ToTable("watchlist");

                entity.HasKey(w => w.Id);

                entity.Property(w => w.Id).HasColumnName("id").ValueGeneratedOnAdd();

                entity.Property(w => w.UserId).HasColumnName("user_id");

                entity.Property(w => w.MediaId).HasColumnName("media_id");

                entity.Property(w => w.MediaType).HasColumnName("media_type");

                entity.Property(w => w.AddedDate).HasColumnName("added_date");

                // Quan hệ với bảng Users
                entity.HasOne(w => w.User)
                          .WithMany(u => u.WatchList)
                          .HasForeignKey(w => w.UserId)
                          .HasConstraintName("FK_WatchList_Users_user_id")
                          .OnDelete(DeleteBehavior.Cascade);

                // Đảm bảo không trùng lặp
                entity.HasIndex(w => new { w.UserId, w.MediaId, w.MediaType })
                          .IsUnique();
            });

            // Cấu hình ánh xạ cho User
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.HasKey(u => u.Id);

                entity.Property(u => u.Id).HasColumnName("id").ValueGeneratedOnAdd();

                entity.Property(u => u.Email).HasColumnName("email");

                entity.Property(u => u.Role).HasColumnName("role");

                entity.Property(u => u.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<Rating>(entity =>
            {
                entity.ToTable("ratings");

                entity.HasKey(r => r.Id);

                entity.Property(r => r.Id).HasColumnName("id").ValueGeneratedOnAdd();

                entity.Property(r => r.UserId).HasColumnName("user_id");

                entity.Property(r => r.MediaId).HasColumnName("media_id");

                entity.Property(r => r.MediaType).HasColumnName("media_type");

                entity.Property(r => r.RatingValue).HasColumnName("rating_value");

                entity.Property(r => r.CreatedAt).HasColumnName("created_at");

                entity.HasOne(r => r.User)
                          .WithMany(u => u.Ratings)
                          .HasForeignKey(r => r.UserId)
                          .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(r => new { r.UserId, r.MediaId, r.MediaType })
                          .IsUnique();  // Mỗi user chỉ được đánh giá 1 lần cho 1 media
            });

            modelBuilder.Entity<Actor>(entity =>
 {
     entity.ToTable("actors");
     entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
     entity.Property(e => e.Name).HasColumnName("name").IsRequired();
     entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
     entity.HasIndex(a => a.Name).IsUnique();
 });

            modelBuilder.Entity<MovieActor>(entity =>
            {
                entity.ToTable("movie_actor"); // Chỉ định rõ tên bảng
                entity.HasKey(ma => new { ma.MovieId, ma.ActorId });
                entity.Property(ma => ma.MovieId).HasColumnName("movie_id");
                entity.Property(ma => ma.ActorId).HasColumnName("actor_id");
                entity.Property(ma => ma.CharacterName).HasColumnName("character_name"); // Thêm ánh xạ

                entity.HasOne(ma => ma.Movie)
                      .WithMany(m => m.MovieActors)
                      .HasForeignKey(ma => ma.MovieId)
                      .OnDelete(DeleteBehavior.Cascade) // Thêm để khớp với schema
                      .IsRequired();

                entity.HasOne(ma => ma.Actor)
                      .WithMany(a => a.MovieActors)
                      .HasForeignKey(ma => ma.ActorId)
                      .OnDelete(DeleteBehavior.Cascade) // Thêm để khớp với schema
                      .IsRequired();
            });

            modelBuilder.Entity<TvSeriesActor>(entity =>
            {
                entity.ToTable("tvseries_actor"); // Chỉ định rõ tên bảng
                entity.HasKey(ta => new { ta.TvSeriesId, ta.ActorId });
                entity.Property(ta => ta.TvSeriesId).HasColumnName("tvseries_id");
                entity.Property(ta => ta.ActorId).HasColumnName("actor_id");
                entity.Property(ta => ta.CharacterName).HasColumnName("character_name"); // Thêm ánh xạ

                entity.HasOne(ta => ta.TvSeries)
                      .WithMany(t => t.TvSeriesActors)
                      .HasForeignKey(ta => ta.TvSeriesId)
                      .OnDelete(DeleteBehavior.Cascade) // Thêm để khớp với schema
                      .IsRequired();

                entity.HasOne(ta => ta.Actor)
                      .WithMany(a => a.TvSeriesActors)
                      .HasForeignKey(ta => ta.ActorId)
                      .OnDelete(DeleteBehavior.Cascade) // Thêm để khớp với schema
                      .IsRequired();
            });
        }
    }
}