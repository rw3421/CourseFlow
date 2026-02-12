namespace CourseFlow.Models
{
    public class Course
    {
        public int Id { get; set; }

        public string CourseCode { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string? Description { get; set; }

        public int CreditHours { get; set; }

        //Lecturer
        public int? staff_id { get; set; }
        public Staff? Staff { get; set; }


        //Time slot
        public string? day_of_week { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

    }
}
