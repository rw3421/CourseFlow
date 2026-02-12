namespace CourseFlow.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Token { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }

        // 🔐 Rotation linkage
        public string? ReplacedByToken { get; set; }
    }
}
