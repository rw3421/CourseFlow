using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CourseFlow.Data;
using CourseFlow.Models.ViewModels;

namespace CourseFlow.Pages.Admin.Logbook
{
    [Authorize(Roles = "ADMIN")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public List<AuditLogViewModel> Logs { get; set; } = new();

        public string? ActionFilter { get; set; }

        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 15;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        public async Task OnGetAsync(string? action, int page = 1)
        {
            ActionFilter = action;
            CurrentPage = page < 1 ? 1 : page;

            var query =
                from log in _context.AuditLogs
                join user in _context.Users
                    on log.UserId equals user.Id into userGroup
                from u in userGroup.DefaultIfEmpty()
                select new AuditLogViewModel
                {
                    Id = log.Id,
                    Action = log.Action,
                    Entity = log.Entity,
                    UserId = log.UserId,
                    UserName = u != null
                        ? (u.FullName ?? u.Email)
                        : "System",
                    IpAddress = log.IpAddress,
                    UserAgent = log.UserAgent,
                    CreatedAt = log.CreatedAt
                };

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(l => l.Action == action);
            }

            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            Logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
    }
}
