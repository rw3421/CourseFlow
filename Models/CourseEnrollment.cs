using System;
using CourseFlow.Models;


namespace CourseFlow.Models
{
    public class CourseEnrollment
    {
        public int Id { get; set; }

        public int StudentId { get; set; }
        public int CourseId { get; set; }

        public string Status { get; set; } = "ENROLLED";

        public DateTime EnrolledAt { get; set; } = DateTime.Now;
        public DateTime? DroppedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // navigation
        public User Student { get; set; } = null!;
        public Course Course { get; set; } = null!;
    }
}
