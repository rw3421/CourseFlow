using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CourseFlow.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using CourseFlow.Models;
using Microsoft.AspNetCore.Mvc;



namespace CourseFlow.Pages.Admin
{
    [Authorize(Roles = "ADMIN")]
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        // ======================
        // KPI Metrics
        // ======================
        public int TotalStaff { get; set; }
        public int TotalStudents { get; set; }
        public int ActiveCourses { get; set; }
        public int PendingRequests { get; set; }

        // ======================
        // Pending Actions
        // ======================
        public List<PendingCourseRequestVM> PendingCourseRequests { get; set; }
            = new();

        public async Task OnGetAsync()
        {
            // ===== KPI Cards =====
            TotalStaff = await _context.Staff.CountAsync();
            TotalStudents = await _context.UserProfiles.CountAsync();
            ActiveCourses = await _context.Courses
                .Where(c => c.IsActive)
                .CountAsync();

            PendingRequests = await _context.CourseApprovals
                .Where(c => c.Status == "PENDING")
                .CountAsync();

            PendingCourseRequests = await _context.CourseApprovals
                .Where(c => c.Status == "PENDING")
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .Select(c => new PendingCourseRequestVM
                {
                    CourseName = $"Course ID: {c.CourseId}",
                    RequestedBy = c.RequestedByRole,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            Announcements = await _context.Announcements
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToListAsync();

            


        }

        // ======================
        // ViewModel
        // ======================
        public class PendingCourseRequestVM
        {
            public string CourseName { get; set; } = string.Empty;
            public string RequestedBy { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        public List<Announcement> Announcements { get; set; } = new();

        [BindProperty]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        public string Message { get; set; } = string.Empty;

        public async Task<IActionResult> OnPostAddAnnouncementAsync()
        {
            var announcement = new Announcement
            {
                Title = Title,
                Message = Message,
                CreatedAt = DateTime.UtcNow
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

    }
}
