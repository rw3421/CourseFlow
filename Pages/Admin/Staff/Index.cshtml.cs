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


namespace CourseFlow.Pages.Admin.Staff
{
    [Authorize(Roles = "ADMIN")]
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
        public List<StaffVM> Staff { get; set; } = new();

        [BindProperty]
        public CreateStaffDto NewStaff { get; set; } = new();

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
                from s in _context.Staff
                join u in _context.Users
                    on s.UserId equals u.Id
                where u.Role == "STAFF" && !u.IsDeleted
                orderby u.IsActive descending
                select new StaffVM
                {
                    Id = s.Id,
                    staff_code = s.staff_code,
                    FullName = s.FullName,
                    Email = s.Email,
                    PhoneNumber = s.PhoneNumber ?? "-",
                    Role = s.Role,
                    Department = s.Department ?? "-",
                    IsActive = s.IsActive,
                    ProfileImagePath =
                        !string.IsNullOrEmpty(s.ProfileImagePath)
                            ? s.ProfileImagePath
                            : "/uploads/staff/default.png"
                };


            // 🔍 SEARCH
            if (!string.IsNullOrWhiteSpace(Search))
            {
                query = query.Where(s =>
                    s.FullName.Contains(Search) ||
                    s.Email.Contains(Search) ||
                    (s.PhoneNumber ?? "").Contains(Search));
            }

            // 🎯 FILTER
            if (Status == "active")
                query = query.Where(s => s.IsActive);
            else if (Status == "inactive")
                query = query.Where(s => !s.IsActive);


            var totalRecords = query.Count();
            TotalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);
            if (TotalPages == 0) TotalPages = 1;

            if (CurrentPage > TotalPages)
                CurrentPage = TotalPages;

            Staff = query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(s => new StaffVM
                {
                    Id = s.Id,
                    staff_code = s.staff_code,
                    FullName = s.FullName,
                    Email = s.Email,
                    PhoneNumber = s.PhoneNumber ?? "-",
                    Role = s.Role,
                    Department = s.Department ?? "-",
                    IsActive = s.IsActive,
                    ProfileImagePath =
                        !string.IsNullOrEmpty(s.ProfileImagePath)
                            ? s.ProfileImagePath
                            : "/uploads/staff/default.png"
                })
                .AsNoTracking()
                .ToList();

            return Page();
        }


        // =====================
        // POST: ADD STAFF
        // =====================
        public async Task<IActionResult> OnPostAdd(
            string staff_code,
            string FullName,
            string email,
            string password,
            string phoneNumber,
            string role,
            string department,
            bool isActive,
            IFormFile? profileImage
        )
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                if (_context.Users.Any(u => u.Email == email))
                {
                    TempData["AddError"] = true;
                    return RedirectToPage();
                }

                var user = new User
                {
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    FullName = FullName,
                    Role = "STAFF",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                string imagePath = "/uploads/staff/default.png";

                if (profileImage != null)
                {
                    var dir = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot/uploads/staff"
                    );

                    Directory.CreateDirectory(dir);

                    var fileName =
                        $"staff_{Guid.NewGuid()}{Path.GetExtension(profileImage.FileName)}";

                    using var fs = new FileStream(
                        Path.Combine(dir, fileName),
                        FileMode.Create
                    );

                    await profileImage.CopyToAsync(fs);

                    imagePath = "/uploads/staff/" + fileName;
                }

                var staff = new CourseFlow.Models.Staff
                {
                    staff_code = staff_code,
                    FullName = FullName,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    Role = role,
                    Department = department,
                    IsActive = isActive,
                    ProfileImagePath = imagePath,
                    UserId = user.Id,
                    CreatedAt = DateTime.Now
                };

                _context.Staff.Add(staff);
                await _context.SaveChangesAsync();

                // ✅ AUDIT: admin created staff
                var adminId = int.Parse(
                    User.FindFirstValue(ClaimTypes.NameIdentifier)!
                );

                await _auditService.LogAsync(
                    adminId,
                    "CREATE",
                    "Staff",
                    staff.Id
                );

                await tx.CommitAsync();

                TempData["AddSuccess"] = true;
            }
            catch
            {
                await tx.RollbackAsync();
                TempData["AddError"] = true;
            }

            return RedirectToPage();
        }




        // =====================
        // POST: EDIT STAFF
        // =====================
        public async Task<IActionResult> OnPostEdit(
            int id,
            string staff_code,
            string FullName,
            string email,
            string phoneNumber,
            string role,
            string department,
            bool isActive,
            IFormFile? profileImage
        )
        {
            var staff = await _context.Staff.FirstOrDefaultAsync(s => s.Id == id);
            if (staff == null)
                return RedirectToPage();

            staff.staff_code = staff_code;
            staff.FullName = FullName;
            staff.Email = email;
            staff.PhoneNumber = phoneNumber;
            staff.Role = role;
            staff.Department = department;
            staff.IsActive = isActive;
            staff.UpdatedAt = DateTime.Now;

            if (profileImage != null && profileImage.Length > 0)
            {
                var uploadsDir = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/staff"
                );

                Directory.CreateDirectory(uploadsDir);

                if (!string.IsNullOrEmpty(staff.ProfileImagePath) &&
                    !staff.ProfileImagePath.Contains("default.png"))
                {
                    var oldPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        staff.ProfileImagePath.TrimStart('/')
                    );

                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var fileName =
                    $"staff_{id}_{Guid.NewGuid()}{Path.GetExtension(profileImage.FileName)}";

                using var stream = new FileStream(
                    Path.Combine(uploadsDir, fileName),
                    FileMode.Create
                );

                await profileImage.CopyToAsync(stream);

                staff.ProfileImagePath = "/uploads/staff/" + fileName;
            }

            await _context.SaveChangesAsync();

            // ✅ AUDIT: admin updated staff
            var adminId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            await _auditService.LogAsync(
                adminId,
                "UPDATE",
                "Staff",
                staff.Id
            );

            TempData["UpdateSuccess"] = true;
            return RedirectToPage();
        }



        // =====================
        // POST: SOFT DELETE
        // =====================
        public async Task<IActionResult> OnPostDelete(int id)
        {
            // 🔐 Get admin user ID
            var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(adminIdClaim, out var adminId))
                return Unauthorized();

            // 1️⃣ Load STAFF + linked USER
            var staff = await _context.Staff
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null || staff.User == null)
                return RedirectToPage();

            // 2️⃣ Soft delete STAFF
            staff.IsActive = false;
            staff.UpdatedAt = DateTime.Now;

            // 3️⃣ Soft delete USER
            staff.User.IsActive = false;
            staff.User.IsDeleted = true;
            staff.User.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // 4️⃣ AUDIT: admin deactivated staff
            await _auditService.LogAsync(
                adminId,
                "DELETE",
                "Staff",
                staff.Id
            );

            TempData["SuccessMessage"] = "Staff deactivated successfully.";
            return RedirectToPage();
        }

    }

    // =====================
    // VIEW MODEL
    // =====================
    public class StaffVM
    {
        public int Id { get; set; }
        public string staff_code { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Role { get; set; } = "";
        public string Department { get; set; } = "";
        public bool IsActive { get; set; }
        public string ProfileImagePath { get; set; } = "";
    }

}
