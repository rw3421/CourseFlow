using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CourseFlow.Data;
using CourseFlow.Models;
using CourseFlow.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using CourseFlow.Services;
using System.Security.Claims;


namespace CourseFlow.Pages.Admin.Students
{
    [Authorize(Roles = "ADMIN,STAFF")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly AuditService _auditService;

        public IndexModel(AppDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // =====================
        // VIEW MODELS
        // =====================
        public List<StudentVM> Students { get; set; } = new();

        [BindProperty]
        public CreateStudentDto NewStudent { get; set; } = new();

        // =====================
        // SEARCH & FILTER
        // =====================
        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        // =====================
        // PAGINATION
        // =====================
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }

        // =====================
        // GET
        // =====================
        public IActionResult OnGet(int pageIndex = 1)
        {
            CurrentPage = pageIndex < 1 ? 1 : pageIndex;

            var query =
                from u in _context.Users
                join p in _context.UserProfiles
                    on u.Id equals p.UserId into up
                from p in up.DefaultIfEmpty()
                where u.Role == "STUDENT" && !u.IsDeleted
                orderby u.IsActive descending
                select new StudentVM
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    Phone = p != null ? p.Phone ?? "-" : "-",
                    DateOfBirth = p != null ? p.DateOfBirth : null,
                    Gender = p != null ? p.Gender : "",
                    Address = p != null ? p.Address : "",
                    profile_image_path =
                        p != null && !string.IsNullOrEmpty(p.profile_image_path)
                            ? p.profile_image_path
                            : "/uploads/students/default.png"
                };

            // 🔍 SEARCH (name / email / phone)
            if (!string.IsNullOrWhiteSpace(Search))
            {
                query = query.Where(s =>
                    s.FullName.Contains(Search) ||
                    s.Email.Contains(Search) ||
                    s.Phone.Contains(Search));
            }

            // 🎯 FILTER
            if (Status == "inactive")
            {
                // show only inactive
                query = query.Where(s => !s.IsActive);
            }
            else if (Status == "active")
            {
                // explicit active
                query = query.Where(s => s.IsActive);
            }

            var totalRecords = query.Count();
            TotalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);

            if (TotalPages == 0)
                TotalPages = 1;

            if (CurrentPage > TotalPages)
                CurrentPage = TotalPages;

            Students = query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .AsNoTracking()
                .ToList();

            return Page();
        }

        // =====================
        // POST: ADD STUDENT
        // =====================
        public async Task<IActionResult> OnPostAdd(
            string FullName,
            string email,
            string password,
            string phone,
            DateTime? dateOfBirth,
            string gender,
            string address,
            bool isActive,
            IFormFile? profileImage
        )
        {
            var actorId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            if (_context.Users.Any(u => u.Email == email))
            {
                TempData["AddError"] = true;
                return RedirectToPage();
            }

            var user = new User
            {
                FullName = FullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "STUDENT",
                IsActive = isActive,
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            string imagePath = "/uploads/students/default.png";

            if (profileImage != null && profileImage.Length > 0)
            {
                var uploadsDir = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/students"
                );

                Directory.CreateDirectory(uploadsDir);

                var uniqueFileName =
                    $"student_{user.Id}_{Guid.NewGuid()}{Path.GetExtension(profileImage.FileName)}";

                using var stream = new FileStream(
                    Path.Combine(uploadsDir, uniqueFileName),
                    FileMode.Create
                );

                await profileImage.CopyToAsync(stream);

                imagePath = "/uploads/students/" + uniqueFileName;
            }

            _context.UserProfiles.Add(new UserProfile
            {
                UserId = user.Id,
                Phone = phone,
                DateOfBirth = dateOfBirth,
                Gender = gender,
                Address = address,
                profile_image_path = imagePath
            });

            await _context.SaveChangesAsync();

            // ✅ AUDIT
            await _auditService.LogAsync(
                actorId,
                "CREATE",
                "Student",
                user.Id
            );

            TempData["AddSuccess"] = true;
            return RedirectToPage();
        }



        // =====================
        // POST: EDIT
        // =====================
        public async Task<IActionResult> OnPostEdit(
            int id,
            string FullName,
            string email,
            bool isActive,
            string phone,
            DateTime? dateOfBirth,
            string gender,
            string address,
            IFormFile? profileImage
        )
        {
            var actorId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                TempData["UpdateError"] = true;
                return RedirectToPage();
            }

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == id);

            if (profile == null)
            {
                profile = new UserProfile
                {
                    UserId = id,
                    profile_image_path = "/uploads/students/default.png"
                };
                _context.UserProfiles.Add(profile);
            }

            user.FullName = FullName;
            user.Email = email;
            user.IsActive = isActive;
            user.UpdatedAt = DateTime.Now;

            profile.Phone = phone;
            profile.DateOfBirth = dateOfBirth;
            profile.Gender = gender;
            profile.Address = address;

            if (profileImage != null && profileImage.Length > 0)
            {
                var uploadsDir = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/students"
                );

                Directory.CreateDirectory(uploadsDir);

                if (!string.IsNullOrEmpty(profile.profile_image_path) &&
                    !profile.profile_image_path.Contains("default.png"))
                {
                    var oldPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        profile.profile_image_path.TrimStart('/')
                    );

                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var uniqueFileName =
                    $"student_{id}_{Guid.NewGuid()}{Path.GetExtension(profileImage.FileName)}";

                using var stream = new FileStream(
                    Path.Combine(uploadsDir, uniqueFileName),
                    FileMode.Create
                );

                await profileImage.CopyToAsync(stream);

                profile.profile_image_path = "/uploads/students/" + uniqueFileName;
            }

            await _context.SaveChangesAsync();

            // ✅ AUDIT
            await _auditService.LogAsync(
                actorId,
                "UPDATE",
                "Student",
                user.Id
            );

            TempData["UpdateSuccess"] = true;
            return RedirectToPage();
        }



        // =====================
        // POST: SOFT DELETE
        // =====================
        public async Task<IActionResult> OnPostDelete(int id)
        {
            var actorId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return RedirectToPage();

            user.IsDeleted = true;
            user.IsActive = false;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // ✅ AUDIT
            await _auditService.LogAsync(
                actorId,
                "DELETE",
                "Student",
                user.Id
            );

            TempData["SuccessMessage"] = "Student deactivated successfully.";
            return RedirectToPage();
        }

    }

    // =====================
    // VIEW MODEL
    // =====================
    public class StudentVM
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsActive { get; set; }
        public string Phone { get; set; } = "";
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; } = "";
        public string Address { get; set; } = "";
        public string profile_image_path { get; set; } = "";
    }
}
