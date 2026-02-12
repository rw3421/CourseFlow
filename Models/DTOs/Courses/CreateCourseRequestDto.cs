using System.ComponentModel.DataAnnotations;

namespace CourseFlow.Models.DTOs.Courses
{
    public class CreateCourseRequestDto
    {
        [Required]
        [MaxLength(20)]
        public string CourseCode { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string CourseName { get; set; } = null!;

        [Range(1, 10)]
        public int CreditHours { get; set; }
    }
}
