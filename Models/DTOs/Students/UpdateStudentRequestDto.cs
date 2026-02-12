using System.ComponentModel.DataAnnotations;

namespace CourseFlow.Models.DTOs.Students
{
    public class UpdateStudentRequestDto
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = null!;
    }
}
