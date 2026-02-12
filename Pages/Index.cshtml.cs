using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CourseFlow.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("ADMIN"))
                    return RedirectToPage("/Admin/Dashboard");

                if (User.IsInRole("STUDENT"))
                    return RedirectToPage("/Dashboard");
            }

            return Page();
        }
    }
}
