using CourseFlow.Data;
using CourseFlow.Models;
using CourseFlow.Services;
using Microsoft.EntityFrameworkCore;

namespace CourseFlow.Services
{
    public class EnrollmentService
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;

        public EnrollmentService(AppDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task EnrollAsync(int studentId, int courseId)
        {
            // 1️⃣ Duplicate enrollment
            if (await _context.CourseEnrollments
                .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId))
                throw new Exception("Already enrolled");

            // 2️⃣ Course exists
            var course = await _context.Courses.FindAsync(courseId)
                ?? throw new Exception("Course not found");

            // 3️⃣ Capacity
            var count = await _context.CourseEnrollments
                .CountAsync(e => e.CourseId == courseId && e.Status == "ENROLLED");

            // 4️⃣ Save
            var enrollment = new CourseEnrollment
            {
                StudentId = studentId,
                CourseId = courseId,
                Status = "ENROLLED",
                EnrolledAt = DateTime.UtcNow
            };

            _context.CourseEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            var student = await _context.Users
                .Where(u => u.Id == studentId)
                .Select(u => new { u.Email })
                .FirstOrDefaultAsync();

            var studentIdentifier = student?.Email ?? $"UserId:{studentId}";

            // Audit
            await _audit.LogAsync(
                studentId,
                "ENROLL_COURSE",
                "course_enrollments",
                courseId
            );
        }

    }
}
