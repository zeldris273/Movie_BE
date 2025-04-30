namespace backend.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; } // Liên kết với user
        public string Token { get; set; } // Refresh token
        public DateTime IssuedAt { get; set; } // Thời gian tạo
        public DateTime ExpiresAt { get; set; } // Thời gian hết hạn
        public bool IsRevoked { get; set; } // Trạng thái thu hồi (dùng khi logout)

        public User User { get; set; } // Liên kết với user
    }
}