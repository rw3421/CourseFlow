using CourseFlow.Data;
using CourseFlow.Models;
using CourseFlow.Models.DTOs.Students;
using CourseFlow.Models.Common;
using CourseFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CourseFlow.Controllers.Api
{
    [ApiController]
    [Route("api/students")]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditService _auditService;

        public StudentsController(
            AppDbContext context,
            AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // ===============================
        // GET: api/students
        // ===============================
        [HttpGet]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> GetAll(int page = 1, int pageSize = 10)
        {
            var students = await _context.Users
                .Where(u => u.Role == "STUDENT" && !u.IsDeleted)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new StudentResponseDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    IsActive = u.IsActive
                })

                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(students));
        }

        // ===============================
        // GET: api/students/{id}
        // ===============================
        [HttpGet("{id}")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> GetById(int id)
        {
            var student = await _context.Users
                .Where(u => u.Id == id && u.Role == "STUDENT")
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.IsActive
                })
                .FirstOrDefaultAsync();

            if (student == null)
                return NotFound(ApiResponse<string>.Fail(
                    "NOT_FOUND",
                    "Student not found"
                ));

            return Ok(ApiResponse<object>.Ok(student));
        }

        // ===============================
        // PUT: api/students/{id}
        // ===============================
        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStudentRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<string>.Fail(
                    "VALIDATION_ERROR",
                    "Invalid input"
                ));

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == "STUDENT");

            if (user == null)
                return NotFound(ApiResponse<string>.Fail(
                    "NOT_FOUND",
                    "Student not found"
                ));

            user.FullName = dto.FullName;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.Ok("Student updated"));
        }

        // ===============================
        // DELETE: api/students/{id}
        // ===============================
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Disable(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == "STUDENT");

            if (user == null)
                return NotFound(ApiResponse<string>.Fail(
                    "NOT_FOUND",
                    "Student not found"
                ));

            user.IsActive = false;
            user.IsDeleted = true;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.Ok("Student disabled"));
        }

        // ===============================
        // GET: api/students/me
        // ===============================
        [HttpGet("me")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> Me()
        {
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var student = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new StudentMeResponseDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    IsActive = u.IsActive
                })

                .FirstOrDefaultAsync();

            if (student == null)
            {
                return NotFound(ApiResponse<string>.Fail(
                    "NOT_FOUND",
                    "Student not found"
                ));
            }

            return Ok(ApiResponse<object>.Ok(student));

        }
    }
}
