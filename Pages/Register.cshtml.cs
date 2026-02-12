using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CourseFlow.Data;
using CourseFlow.Models;
using System.Linq;

namespace CourseFlow.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _context;

        public RegisterModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string FullName { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string? Message { get; set; }

        [TempData]
        public bool RegisterSuccess { get; set; }

        // IMPORTANT: clear flag on normal GET
        public void OnGet()
        {
            RegisterSuccess = false;
        }

        public IActionResult OnPost()
        {
            if (_context.Users.Any(u => u.Email == Email))
            {
                Message = "Email already registered";
                return Page();
            }

            var user = new User
            {
                FullName = FullName,
                Email = Email,
                PasswordHash = Password,
                IsActive = true
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            RegisterSuccess = true;
            return Page(); // alert + redirect handled in UI
        }
    }
}
