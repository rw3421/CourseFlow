namespace CourseFlow.Models.ViewModels
{
    public class AuditLogViewModel
    {
        public int Id { get; set; }

        public string Action { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;

        public int? UserId { get; set; }
        public string UserName { get; set; } = "System";

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
