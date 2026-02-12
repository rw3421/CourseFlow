using System.ComponentModel.DataAnnotations;

namespace CourseFlow.DTOs
{
    public class UpdateStudentDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = null!;

        [EmailAddress]
        public string? Email { get; set; }

        public bool IsActive { get; set; }
    }
}
