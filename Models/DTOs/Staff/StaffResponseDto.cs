namespace CourseFlow.Models.DTOs.Staff
{
    public class StaffResponseDto
    {
        public int Id { get; set; }
        public string StaffCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Department { get; set; }
        public bool IsActive { get; set; }
    }
}
