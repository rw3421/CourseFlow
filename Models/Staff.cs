using System;

namespace CourseFlow.Models
{
    public class Staff
    {
        public int Id { get; set; }

        public string staff_code { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhoneNumber { get; set; }

        public string Role { get; set; } = "";
        public string? Department { get; set; }

        public string? ProfileImagePath { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
    }

}
