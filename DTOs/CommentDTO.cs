using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.DTOs
{
    public class CommentResponseDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public int? MovieId { get; set; }
        public int? TvSeriesId { get; set; }
        public int? EpisodeId { get; set; }
        public int? ParentCommentId { get; set; } // Thêm ParentCommentId
        public List<CommentResponseDTO> Replies { get; set; } = new List<CommentResponseDTO>();
        public string CommentText { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class CommentRequestDTO
    {
        public int UserId { get; set; }
        public int? MovieId { get; set; }
        public int? TvSeriesId { get; set; }
        public int? EpisodeId { get; set; }
        public int? ParentCommentId { get; set; } 
        public string CommentText { get; set; }
    }
}