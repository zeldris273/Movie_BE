using System;

namespace backend.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public CustomUser User { get; set; }
        public int? MovieId { get; set; }
        public Movie Movie { get; set; }
        public int? TvSeriesId { get; set; }
        public TvSeries TvSeries { get; set; }
        public int? EpisodeId { get; set; }
        public Episode Episode { get; set; }
        public int? ParentCommentId { get; set; } // Thêm ParentCommentId
        public Comment ParentComment { get; set; } // Quan hệ với bình luận cha
        public List<Comment> Replies { get; set; } = new List<Comment>(); // Danh sách các bình luận trả lời
        public string CommentText { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}