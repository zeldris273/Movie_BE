using System;

namespace backend.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int? MovieId { get; set; }
        public Movie Movie { get; set; }
        public int? TvSeriesId { get; set; }
        public TvSeries TvSeries { get; set; }
        public int? EpisodeId { get; set; }
        public Episode Episode { get; set; }
        public string CommentText { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}