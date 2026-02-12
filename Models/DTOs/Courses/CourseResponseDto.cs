public class CourseResponseDto
{
    public int Id { get; set; }
    public string CourseCode { get; set; } = null!;
    public string CourseName { get; set; } = null!;
    public int CreditHours { get; set; }

    public int? LecturerId { get; set; }
    public string? LecturerName { get; set; }
}
