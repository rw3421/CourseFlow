using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CourseFlow.Data;
using CourseFlow.Services;
using Microsoft.EntityFrameworkCore;

namespace CourseFlow.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly AuditService _auditService;

        // ✅ FIX: inject AuditService
        public LoginModel(AppDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [BindProperty] public string Email { get; set; } = string.Empty;
        [BindProperty] public string Password { get; set; } = string.Empty;

        public string? Message { get; set; }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("ADMIN"))
                    return RedirectToPage("/Admin/Dashboard");

                if (User.IsInRole("STAFF"))
                    return RedirectToPage("/Staff/Index");

                if (User.IsInRole("STUDENT"))
                    return RedirectToPage("/Dashboard");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                Message = "Please enter email and password";
                return Page();
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.Email == Email &&
                    u.IsActive &&
                    !u.IsDeleted
                );

            if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
            {
                Message = "Invalid email or password";
                return Page();
            }

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var role = user.Role.Trim().ToUpperInvariant();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = false
                }
            );

            // ✅ FIX: WRITE AUDIT LOG
            await _auditService.LogAsync(
                user.Id,
                "LOGIN",
                user.Role
            );

            return role switch
            {
                "ADMIN" => RedirectToPage("/Admin/Dashboard"),
                "STAFF" => RedirectToPage("/Staff/Index"),
                "STUDENT" => RedirectToPage("/Dashboard"),
                _ => RedirectToPage("/Index")
            };
        }
    }
}
