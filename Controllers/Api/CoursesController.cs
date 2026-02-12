using CourseFlow.Data;
using CourseFlow.Models;
using CourseFlow.Models.Common;
using CourseFlow.Models.DTOs.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CourseFlow.Services;

namespace CourseFlow.Controllers.Api
{
    [ApiController]
    [Route("api/courses")]
    [Authorize]
    public class CoursesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditService _auditService;

        // ✅ CORRECT constructor
        public CoursesController(
            AppDbContext context,
            AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // ===============================
        // GET: api/courses
        // ===============================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var courses = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Staff)
                .Where(c => c.IsActive)
                .Select(c => new CourseResponseDto
                {
                    Id = c.Id,
                    CourseCode = c.CourseCode,
                    CourseName = c.CourseName,
                    CreditHours = c.CreditHours,

                    // 🔴 ADD THESE
                    LecturerId = c.staff_id,
                    LecturerName = c.Staff != null ? c.Staff.FullName : null
                })
                .ToListAsync();


            return Ok(ApiResponse<List<CourseResponseDto>>.Ok(courses));
        }

        // ===============================
        // GET: api/courses/{id}
        // ===============================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .Where(c => c.Id == id && c.IsActive)
                .Select(c => new CourseResponseDto
                {
                    Id = c.Id,
                    CourseCode = c.CourseCode,
                    CourseName = c.CourseName,
                    CreditHours = c.CreditHours
                })
                .FirstOrDefaultAsync();

            if (course == null)
            {
                return NotFound(ApiResponse<string>.Fail(
                    "COURSE_NOT_FOUND",
                    "Course not found"
                ));
            }

            return Ok(ApiResponse<CourseResponseDto>.Ok(course));
        }

        // ===============================
        // POST: api/courses
        // ===============================
        [HttpPost]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> Create(
            [FromBody] CreateCourseRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail(
                    "VALIDATION_ERROR",
                    "Invalid course data"
                ));
            }

            bool codeExists = await _context.Courses
                .AnyAsync(c => c.CourseCode == dto.CourseCode);

            if (codeExists)
            {
                return Conflict(ApiResponse<string>.Fail(
                    "DUPLICATE_COURSE_CODE",
                    "Course code already exists"
                ));
            }

            var course = new Course
            {
                CourseCode = dto.CourseCode.Trim(),
                CourseName = dto.CourseName.Trim(),
                CreditHours = dto.CreditHours,
                IsActive = true
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var response = new CourseResponseDto
            {
                Id = course.Id,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                CreditHours = course.CreditHours
            };

            return CreatedAtAction(
                nameof(GetById),
                new { id = course.Id },
                ApiResponse<CourseResponseDto>.Ok(response)
            );
        }

        // ===============================
        // PUT: api/courses/{id}
        // ===============================
        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateCourseRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail(
                    "VALIDATION_ERROR",
                    "Invalid course data"
                ));
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null || !course.IsActive)
            {
                return NotFound(ApiResponse<string>.Fail(
                    "COURSE_NOT_FOUND",
                    "Course not found"
                ));
            }

            course.CourseName = dto.CourseName.Trim();
            course.CreditHours = dto.CreditHours;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.Ok("Course updated successfully"));
        }

        // ===============================
        // DELETE: api/courses/{id}
        // ===============================
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Disable(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null || !course.IsActive)
            {
                return NotFound(ApiResponse<string>.Fail(
                    "COURSE_NOT_FOUND",
                    "Course not found"
                ));
            }

            course.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.Ok("Course disabled successfully"));
        }
    }
}
