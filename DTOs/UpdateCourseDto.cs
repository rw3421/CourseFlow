using System.ComponentModel.DataAnnotations;

namespace CourseFlow.DTOs
{
    public class UpdateCourseDto
    {
        [Required]
        [StringLength(100)]
        public string CourseName { get; set; } = null!;

        [Range(1, 10)]
        public int Credit { get; set; }

        public bool IsActive { get; set; }
    }
}
