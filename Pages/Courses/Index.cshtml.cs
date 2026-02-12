using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using CourseFlow.Data;
using CourseFlow.Models;
using System.Security.Claims;
using CourseFlow.Services;


namespace CourseFlow.Pages.Courses
{
    
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
        public List<CourseVM> Courses { get; set; } = new();

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
        public async Task<IActionResult> OnGetAsync(int pageIndex = 1)
        {
            CurrentPage = pageIndex < 1 ? 1 : pageIndex;

            IQueryable<Course> query = _context.Courses
                .Include(c => c.Staff)
                .AsQueryable();

            // ---------------------
            // Load staff list
            // ---------------------
            StaffList = await _context.Staff
                .OrderBy(s => s.FullName)
                .AsNoTracking()
                .ToListAsync();

            // ---------------------
            // AUTH: GET USER ID
            // ---------------------
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            // ---------------------
            // STAFF: filter courses
            // ---------------------
            if (User.IsInRole("STAFF"))
            {
                var staffId = await _context.Staff
                    .Where(s => s.UserId == userId)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync();

                if (staffId == 0)
                    return Forbid();

                query = query.Where(c => c.staff_id == staffId);
            }

            // ---------------------
            // ADMIN: pending approvals
            // ---------------------
            if (User.IsInRole("ADMIN"))
            {
                PendingApprovals = await (
                    from a in _context.CourseApprovals
                    join s in _context.Staff on a.RequestedById equals s.UserId
                    where a.Status == "PENDING"
                    orderby a.CreatedAt descending
                    select new CourseApprovalVM
                    {
                        Id = a.Id,
                        CourseId = a.CourseId,
                        ActionType = a.ActionType,
                        StaffName = s.FullName,
                        CreatedAt = a.CreatedAt,
                        PayloadJson = a.PayloadJson,
                        CourseCode = "",
                        CourseName = ""
                    }
                ).AsNoTracking().ToListAsync();

                foreach (var p in PendingApprovals)
                {
                    var payload = JsonSerializer.Deserialize<Course>(p.PayloadJson);
                    if (payload != null)
                    {
                        p.CourseCode = payload.CourseCode;
                        p.CourseName = payload.CourseName;
                    }
                }
            }

            // ---------------------
            // SEARCH
            // ---------------------
            if (!string.IsNullOrWhiteSpace(Search))
            {
                query = query.Where(c =>
                    c.CourseCode.Contains(Search) ||
                    c.CourseName.Contains(Search));
            }

            // ---------------------
            // FILTER
            // ---------------------
            if (Status == "active")
                query = query.Where(c => c.IsActive);
            else if (Status == "inactive")
                query = query.Where(c => !c.IsActive);

            // ---------------------
            // PAGINATION
            // ---------------------
            query = query
                .OrderByDescending(c => c.IsActive)
                .ThenBy(c => c.CourseName);

            var totalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);
            if (TotalPages == 0) TotalPages = 1;

            if (CurrentPage > TotalPages)
                CurrentPage = TotalPages;

            Courses = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(c => new CourseVM
                {
                    Id = c.Id,
                    CourseName = c.CourseName,
                    CourseCode = c.CourseCode,
                    Description = c.Description ?? "-",
                    CreditHours = c.CreditHours,
                    IsActive = c.IsActive,
                    StaffId = c.staff_id,
                    StaffName = c.Staff != null ? c.Staff.FullName : "-",
                    day_of_week = c.day_of_week ?? "-",
                    StartTime = c.StartTime.HasValue
                        ? c.StartTime.Value.ToString(@"hh\:mm")
                        : "-",
                    EndTime = c.EndTime.HasValue
                        ? c.EndTime.Value.ToString(@"hh\:mm")
                        : "-"
                })
                .AsNoTracking()
                .ToListAsync();

            // ---------------------
            // STUDENT: enrolled courses
            // ---------------------
            if (User.IsInRole("STUDENT"))
            {
                var enrolledList = await _context.CourseEnrollments
                    .Where(e =>
                        e.StudentId == userId &&
                        e.Status == "ENROLLED" &&
                        e.DroppedAt == null)
                    .Select(e => e.CourseId)
                    .ToListAsync();

                EnrolledCourseIds = enrolledList.ToHashSet();
            }

            return Page();
        }


        public int? CourseId { get; set; }
        // =====================
        // POST: ADD COURSE
        // =====================
        public IActionResult OnPostAdd(
            string courseCode,
            string courseName,
            string? description,
            int creditHours,
            string day_of_week,
            TimeSpan startTime,
            TimeSpan EndTime
        )
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            if (User.IsInRole("ADMIN"))
            {
                var course = new Course
                {
                    CourseCode = courseCode,
                    CourseName = courseName,
                    Description = description,
                    CreditHours = creditHours,
                    day_of_week = day_of_week,
                    StartTime = startTime,
                    EndTime = EndTime,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Courses.Add(course);
                _context.SaveChanges(); // 🔑 get Course.Id

                // ✅ AUDIT: admin created course
                _auditService.LogAsync(
                    userId,
                    "CREATE",
                    "Course",
                    course.Id
                ).Wait();

                TempData["ToastMessage"] = "Success: Course added successfully.";
            }
            else
            {
                var staffId = _context.Staff
                    .Where(s => s.UserId == userId)
                    .Select(s => s.Id)
                    .FirstOrDefault();

                if (staffId == 0)
                {
                    return Forbid();
                }

                var payload = new Course
                {
                    CourseCode = courseCode,
                    CourseName = courseName,
                    Description = description,
                    CreditHours = creditHours,
                    staff_id = staffId,
                    day_of_week = day_of_week,
                    StartTime = startTime,
                    EndTime = EndTime,
                    IsActive = true
                };

                var approval = new CourseApproval
                {
                    CourseId = null,
                    ActionType = "ADD",
                    PayloadJson = JsonSerializer.Serialize(payload),
                    RequestedById = userId,
                    RequestedByRole = "STAFF",
                    Status = "PENDING",
                    CreatedAt = DateTime.Now
                };

                _context.CourseApprovals.Add(approval);
                _context.SaveChanges(); // 🔑 get Approval.Id

                // AUDIT: staff submitted course
                _auditService.LogAsync(
                    userId,
                    "SUBMIT",
                    "CourseApproval",
                    approval.Id
                ).Wait();

                TempData["ToastMessage"] = "Success: Course submitted for admin approval.";
            }

            return RedirectToPage();
        }


        // =====================
        // POST: EDIT COURSE
        // =====================
        public async Task<IActionResult> OnPostEdit(
            int id,
            string courseCode,
            string courseName,
            string? description,
            int creditHours,
            bool isActive,
            int staffId,
            string dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime
        )
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            if (User.IsInRole("ADMIN"))
            {
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
                if (course == null) return RedirectToPage();

                course.CourseCode = courseCode;
                course.CourseName = courseName;
                course.Description = description;
                course.CreditHours = creditHours;
                course.IsActive = isActive;
                course.staff_id = staffId;
                course.day_of_week = dayOfWeek;
                course.StartTime = startTime;
                course.EndTime = endTime;
                course.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // ✅ AUDIT: admin updated course
                await _auditService.LogAsync(
                    userId,
                    "UPDATE",
                    "Course",
                    course.Id
                );

                TempData["ToastMessage"] = "Success: Course updated successfully.";
            }
            else
            {
                var currentStaffId = await _context.Staff
                    .Where(s => s.UserId == userId)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync();

                if (currentStaffId == 0)
                    return Forbid();

                var ownsCourse = await _context.Courses
                    .AnyAsync(c => c.Id == id && c.staff_id == currentStaffId);

                if (!ownsCourse)
                    return Forbid();

                if (HasPendingRequest(id))
                {
                    TempData["ToastMessage"] = "Error: This course already has a pending request.";
                    return RedirectToPage();
                }

                var payload = new Course
                {
                    Id = id,
                    CourseCode = courseCode,
                    CourseName = courseName,
                    Description = description,
                    CreditHours = creditHours,
                    IsActive = isActive,
                    staff_id = currentStaffId,
                    day_of_week = dayOfWeek,
                    StartTime = startTime,
                    EndTime = endTime
                };

                var approval = new CourseApproval
                {
                    CourseId = id,
                    ActionType = "EDIT",
                    PayloadJson = JsonSerializer.Serialize(payload),
                    RequestedById = userId,
                    RequestedByRole = "STAFF",
                    Status = "PENDING",
                    CreatedAt = DateTime.Now
                };

                _context.CourseApprovals.Add(approval);
                await _context.SaveChangesAsync();

                // ✅ AUDIT: staff submitted edit
                await _auditService.LogAsync(
                    userId,
                    "SUBMIT_EDIT",
                    "CourseApproval",
                    approval.Id
                );

                TempData["ToastMessage"] = "Success: Course update submitted for admin approval.";
            }

            return RedirectToPage();
        }

        // =====================
        // POST: SOFT DELETE
        // =====================
        public async Task<IActionResult> OnPostDelete(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            if (User.IsInRole("ADMIN"))
            {
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
                if (course == null) return RedirectToPage();

                course.IsActive = false;
                course.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // ✅ AUDIT: admin deleted course
                await _auditService.LogAsync(
                    userId,
                    "DELETE",
                    "Course",
                    course.Id
                );

                TempData["ToastMessage"] = "Success: Course deactivated successfully.";
            }
            else
            {
                var approval = new CourseApproval
                {
                    CourseId = id,
                    ActionType = "DELETE",
                    PayloadJson = JsonSerializer.Serialize(new { CourseId = id }),
                    RequestedById = userId,
                    RequestedByRole = "STAFF",
                    Status = "PENDING",
                    CreatedAt = DateTime.Now
                };

                _context.CourseApprovals.Add(approval);
                await _context.SaveChangesAsync();

                // ✅ AUDIT: staff submitted delete
                await _auditService.LogAsync(
                    userId,
                    "SUBMIT_DELETE",
                    "CourseApproval",
                    approval.Id
                );

                TempData["ToastMessage"] = "Success: Course deactivation submitted for admin approval.";
            }

            return RedirectToPage();
        }

        // =====================
        // POST: STUDENT ENROLL
        // =====================
        public async Task<IActionResult> OnPostEnroll(int courseId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var studentId))
                return Unauthorized();

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null || !course.IsActive)
            {
                TempData["ToastMessage"] = "Error: Course not found or inactive.";
                return RedirectToPage();
            }

            var hasClash = await _context.CourseEnrollments
                .Where(e =>
                    e.StudentId == studentId &&
                    e.Status == "ENROLLED" &&
                    e.CourseId != courseId)
                .Join(
                    _context.Courses,
                    e => e.CourseId,
                    c => c.Id,
                    (e, c) => c
                )
                .AnyAsync(c =>
                    c.day_of_week == course.day_of_week &&
                    c.StartTime < course.EndTime &&
                    c.EndTime > course.StartTime
                );

            if (hasClash)
            {
                TempData["ToastMessage"] = "Error: Schedule clash detected with another enrolled subject.";
                return RedirectToPage();
            }

            var enrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e =>
                    e.StudentId == studentId &&
                    e.CourseId == courseId);

            if (enrollment != null)
            {
                if (enrollment.Status == "ENROLLED")
                {
                    TempData["ToastMessage"] = "Error: You are already enrolled in this course.";

                    return RedirectToPage();
                }

                enrollment.Status = "ENROLLED";
                enrollment.EnrolledAt = DateTime.Now;
            }
            else
            {
                enrollment = new CourseEnrollment
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    Status = "ENROLLED",
                    EnrolledAt = DateTime.Now,
                    CreatedAt = DateTime.Now
                };

                _context.CourseEnrollments.Add(enrollment);
            }

            await _context.SaveChangesAsync();

            // ✅ AUDIT: student enrolled
            await _auditService.LogAsync(
                studentId,
                "ENROLL",
                "CourseEnrollment",
                enrollment.Id
            );

            TempData["ToastMessage"] = "Success: Successfully enrolled!";
            return RedirectToPage();
        }

        // =====================
        // POST: STUDENT UNENROLL
        // =====================
        public async Task<IActionResult> OnPostUnenroll(int courseId)
        {
            // ---------------------
            // Get student ID
            // ---------------------
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var studentId))
                return Unauthorized();

            // ---------------------
            // Find active enrollment
            // ---------------------
            var enrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e =>
                    e.StudentId == studentId &&
                    e.CourseId == courseId &&
                    e.Status == "ENROLLED" &&
                    e.DroppedAt == null);

            if (enrollment == null)
            {
                TempData["ToastMessage"] =
                    "Error: You are not currently enrolled in this course.";
                return RedirectToPage();
            }

            // ---------------------
            // Soft-unenroll (schema correct)
            // ---------------------
            enrollment.DroppedAt = DateTime.Now;
            enrollment.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // ---------------------
            // AUDIT LOG
            // ---------------------
            await _auditService.LogAsync(
                studentId,
                "UNENROLL",
                "CourseEnrollment",
                enrollment.Id
            );

            TempData["ToastMessage"] =
                "Success: You have successfully unenrolled from the course.";

            return RedirectToPage();
        }


        public List<CourseApprovalVM> PendingApprovals { get; set; } = new();

        public class CourseApprovalVM
        {
            public int Id { get; set; }
            public int? CourseId { get; set; }
            public string ActionType { get; set; } = "";
            public string CourseCode { get; set; } = "";
            public string CourseName { get; set; } = "";
            public string StaffName { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public string PayloadJson { get; set; } = "";
        }

        public IList<CourseFlow.Models.Staff> StaffList { get; set; } = new List<CourseFlow.Models.Staff>();

        public int PendingCount => PendingApprovals.Count;

        public IActionResult OnPostApprove(
            int approvalId,
            string decision,
            string courseCode,
            int creditHours,
            string courseName,
            string description
        )
        {
            var approval = _context.CourseApprovals
                .FirstOrDefault(a => a.Id == approvalId);

            if (approval == null) 
                return RedirectToPage();

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            if (decision == "REJECT")
            {
                approval.Status = "REJECTED";
                approval.ReviewedById = userId;
                approval.ReviewedAt = DateTime.Now;
                _context.SaveChanges();
                return RedirectToPage();
            }

            var payload = JsonSerializer.Deserialize<Course>(approval.PayloadJson)
              ?? throw new InvalidOperationException("PayloadJson is invalid");

            
            payload.CourseCode = courseCode;
            payload.CreditHours = creditHours;
            payload.CourseName = courseName;
            payload.Description = description;

            // overwrite payload
            approval.PayloadJson = JsonSerializer.Serialize(payload);
            if (payload == null)
                return BadRequest("Invalid approval payload.");

            switch (approval.ActionType)
            {
                case "ADD":
                    payload.Id = 0;
                    payload.IsActive = true;
                    payload.CreatedAt = DateTime.Now;

                    _context.Courses.Add(payload);
                    _context.SaveChanges();

                    approval.CourseId = payload.Id;
                    break;

                case "EDIT":
                    var course = _context.Courses.FirstOrDefault(c => c.Id == approval.CourseId);
                    if (course == null) return NotFound();

                    course.CourseCode = payload.CourseCode;
                    course.CourseName = payload.CourseName;
                    course.Description = payload.Description;
                    course.CreditHours = payload.CreditHours;
                    course.staff_id = payload.staff_id;
                    course.day_of_week = payload.day_of_week;
                    course.StartTime = payload.StartTime;
                    course.EndTime = payload.EndTime;
                    course.UpdatedAt = DateTime.Now;
                    break;

                case "DELETE":
                    var toDelete = _context.Courses.FirstOrDefault(c => c.Id == approval.CourseId);
                    if (toDelete != null)
                    {
                        toDelete.IsActive = false;
                        toDelete.UpdatedAt = DateTime.Now;
                    }
                    break;
            }

            approval.Status = "APPROVED";
            approval.ReviewedById = userId;
            approval.ReviewedAt = DateTime.Now;

            _context.SaveChanges();
            return RedirectToPage();
        }


        private bool HasPendingRequest(int courseId)
        {
            return _context.CourseApprovals.Any(a =>
                a.CourseId == courseId &&
                a.Status == "PENDING"
            );
        }

        public HashSet<int> EnrolledCourseIds { get; set; } = new();

        public class CourseVM
        {
            public int Id { get; set; }
            public string? CourseName { get; set; }
            public string? CourseCode { get; set; }
            public string? Description { get; set; }
            public int CreditHours { get; set; }
            public bool IsActive { get; set; }
            public int? StaffId { get; set; }

            public string? StaffName { get; set; }

            public string? day_of_week { get; set; }
            public string? StartTime { get; set; }
            public string? EndTime { get; set; }
        }


    }
}
