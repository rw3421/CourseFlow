using System.ComponentModel.DataAnnotations;

namespace CourseFlow.DTOs
{
    public class CreateCourseDto
    {
        [Required]
        [StringLength(100)]
        public string CourseName { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string CourseCode { get; set; } = null!;

        [Required]
        [Range(1, 10)]
        public int Credit { get; set; }
    }
}
