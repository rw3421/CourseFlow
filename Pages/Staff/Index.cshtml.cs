using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CourseFlow.Data;
using CourseFlow.Models;

namespace CourseFlow.Pages.Staff
{
    [Authorize(Roles = "STAFF")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        // =====================
        // VIEW DATA
        // =====================
        public Models.Staff Staff { get; set; } = null!;
        public int PendingApprovalCount { get; set; }
        public int CourseCount { get; set; }
        public List<Course> Courses { get; set; } = new();
        public List<Announcement> Announcements { get; set; } = new();

        // =====================
        // GET
        // =====================
        public IActionResult OnGet()
        {
            // 🔐 Get user ID
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            // 🔗 Map User → Staff
            var staffId = _context.Staff
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .FirstOrDefault();

            if (staffId == 0)
            {
                return Forbid();
            }

            // 👤 Load Staff Profile
            Staff = _context.Staff
                .AsNoTracking()
                .FirstOrDefault(s => s.Id == staffId)
                ?? throw new InvalidOperationException("Staff not found");


            if (Staff == null)
            {
                return Forbid();
            }

            // 📚 Load Staff Courses
            Courses = _context.Courses
                .Where(c => c.staff_id == staffId)
                .AsNoTracking()
                .ToList();

            CourseCount = Courses.Count;

            // ⏳ Pending Approval Count (this staff only)
            PendingApprovalCount = _context.CourseApprovals
                .Count(a =>
                    a.RequestedById == userId &&
                    a.Status == "PENDING"
                );

            // 📢 Announcements
            Announcements = _context.Announcements
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .AsNoTracking()
                .ToList();

            return Page();
        }
    }
}
