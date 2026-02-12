using System;

namespace CourseFlow.Models
{
    public class UserProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }

        // =========================
        // 🔒 SOFT DELETE + AUDIT
        // =========================
        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // 🔽 NAVIGATION
        public User? User { get; set; }

        public string? profile_image_path { get; set; }

    }
}
