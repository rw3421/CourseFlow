using System;
using System.ComponentModel.DataAnnotations;

namespace CourseFlow.Models.DTOs
{
    public class CreateStaffDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(50)]
        public string? Role { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
