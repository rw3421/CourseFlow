using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using CourseFlow.Data;
using CourseFlow.Models;
using System.Security.Claims;

namespace CourseFlow.Pages
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProfileModel(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public User CurrentUser { get; private set; } = null!;
        public UserProfile? Profile { get; private set; }

        /* ======================
           HELPERS
        ====================== */
        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out userId);
        }

        private async Task<IActionResult> ForceLogout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );
            return RedirectToPage("/Login");
        }

        /* ======================
           GET
        ====================== */
        public async Task<IActionResult> OnGetAsync()
        {
            if (!TryGetUserId(out var userId))
                return await ForceLogout();

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return await ForceLogout();

            CurrentUser = user;

            Profile = _context.UserProfiles
                .FirstOrDefault(p => p.UserId == userId);

            return Page();
        }

        /* ======================
           UPDATE PROFILE (WITH IMAGE)
        ====================== */
        public async Task<IActionResult> OnPostUpdateProfileAsync(
            string FullName,
            string phone,
            DateTime? dateOfBirth,
            string gender,
            string address,
            IFormFile? profileImage
        )
        {
            if (!TryGetUserId(out var userId))
                return await ForceLogout();

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return await ForceLogout();

            user.FullName = FullName;

            var profile = _context.UserProfiles
                .FirstOrDefault(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new UserProfile
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserProfiles.Add(profile);
            }

            /* ===== IMAGE UPLOAD ===== */
            if (profileImage != null && profileImage.Length > 0)
            {
                // basic validation
                if (!profileImage.ContentType.StartsWith("image/"))
                {
                    TempData["Error"] = "Only image files are allowed.";
                    return RedirectToPage();
                }

                if (profileImage.Length > 2 * 1024 * 1024)
                {
                    TempData["Error"] = "Image must be less than 2MB.";
                    return RedirectToPage();
                }

                var uploadDir = Path.Combine(
                    _env.WebRootPath,
                    "uploads",
                    "students"
                );

                Directory.CreateDirectory(uploadDir);

                var fileName =
                    $"{Guid.NewGuid()}{Path.GetExtension(profileImage.FileName)}";

                var filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                // store SAFE relative path
                profile.profile_image_path = "/uploads/students/" + fileName;
            }

            /* ===== OTHER FIELDS ===== */
            profile.Phone = phone;
            profile.DateOfBirth = dateOfBirth;
            profile.Gender = gender;
            profile.Address = address;
            profile.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            TempData["Success"] = "Profile updated successfully";
            return RedirectToPage();
        }

        /* ======================
           CHANGE PASSWORD
        ====================== */
        public async Task<IActionResult> OnPostChangePasswordAsync(
            string currentPassword,
            string newPassword,
            string confirmPassword
        )
        {
            if (!TryGetUserId(out var userId))
                return await ForceLogout();

            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["Error"] = "All password fields are required";
                return RedirectToPage();
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match";
                return RedirectToPage();
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                TempData["Error"] = "Account error. Please contact administrator.";
                return RedirectToPage();
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                TempData["Error"] = "Current password is incorrect";
                return RedirectToPage();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.SaveChanges();

            TempData["Success"] = "Password updated successfully";
            return RedirectToPage();
        }
    }
}
