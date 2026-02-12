namespace CourseFlow.Models.DTOs.Students
{
    public class StudentMeResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
