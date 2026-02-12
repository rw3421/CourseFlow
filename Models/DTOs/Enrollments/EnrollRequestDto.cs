using System.ComponentModel.DataAnnotations;

namespace CourseFlow.Models.DTOs.Enrollments
{
    public class EnrollRequestDto
    {
        [Required]
        public int CourseId { get; set; }
    }
}
