namespace CourseFlow.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Action { get; set; } = null!;
        public string Entity { get; set; } = null!;
        public int? EntityId { get; set; }

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; }

    }
}
