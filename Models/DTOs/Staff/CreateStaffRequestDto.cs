using System.ComponentModel.DataAnnotations;

namespace CourseFlow.Models.DTOs.Staff
{
    public class CreateStaffRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = null!;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(50)]
        public string? Department { get; set; }
    }
}
