using System;

namespace CourseFlow.Models
{
    public class CourseApproval
    {
        public int Id { get; set; }
        public int? CourseId { get; set; }

        public string ActionType { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = string.Empty;

        public int RequestedById { get; set; }
        public string RequestedByRole { get; set; } = string.Empty;

        public string Status { get; set; } = "PENDING";

        public int? ReviewedById { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; }
    }

}
