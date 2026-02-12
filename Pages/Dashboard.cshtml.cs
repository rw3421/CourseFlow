using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CourseFlow.Data;
using CourseFlow.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace CourseFlow.Pages
{
    [Authorize(Roles = "STUDENT")]
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public string StudentName { get; set; } = string.Empty;

        public int EnrolledCourseCount { get; set; }

        public int TotalCreditHours { get; set; }

        public List<Course> EnrolledCourses { get; set; } = new();

        public List<Announcement> Announcements { get; set; } = new();

        public void OnGet()
        {
            // ============================
            // GET STUDENT USER ID
            // ============================
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var studentUserId))
                return;

            // ============================
            // STUDENT NAME
            // ============================
            StudentName =
                User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? "Student";

            // ============================
            // ENROLLED COURSES
            // ============================
            EnrolledCourses = _context.CourseEnrollments
                .Where(e =>
                    e.StudentId == studentUserId &&
                    e.DroppedAt == null &&
                    e.Status == "ENROLLED")
                .Join(
                    _context.Courses,
                    e => e.CourseId,
                    c => c.Id,
                    (e, c) => c
                )
                .OrderBy(c => c.day_of_week)
                .ThenBy(c => c.StartTime)
                .ToList();

            EnrolledCourseCount = EnrolledCourses.Count;

            TotalCreditHours = EnrolledCourses.Sum(c => c.CreditHours);

            // ============================
            // ANNOUNCEMENTS
            // ============================
            Announcements = _context.Announcements
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToList();
        }
    }
}
