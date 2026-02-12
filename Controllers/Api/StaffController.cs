using CourseFlow.Data;
using CourseFlow.Models;
using CourseFlow.Models.DTOs.Staff;
using CourseFlow.Models.Common;
using CourseFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CourseFlow.Controllers.Api
{
    [ApiController]
    [Route("api/staff")]
    [Authorize(Roles = "ADMIN")]
    public class StaffController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditService _auditService;

        public StaffController(
            AppDbContext context,
            AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // ===============================
        // GET: api/staff
        // ===============================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var staffList = await _context.Staff
                .Where(s => s.IsActive)
                .Select(s => new StaffResponseDto
                {
                    Id = s.Id,
                    StaffCode = s.staff_code,
                    FullName = s.FullName,
                    Email = s.Email,
                    PhoneNumber = s.PhoneNumber,
                    Department = s.Department,
                    IsActive = s.IsActive
                })

                .ToListAsync();

            return Ok(ApiResponse<List<StaffResponseDto>>.Ok(staffList));
        }

        // ===============================
        // POST: api/staff
        // ===============================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStaffRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<string>.Fail(
                    "VALIDATION_ERROR",
                    "Invalid input"
                ));

            // 1️⃣ Check duplicate email (USERS table)
            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == dto.Email);

            if (emailExists)
                return BadRequest(ApiResponse<string>.Fail(
                    "DUPLICATE_EMAIL",
                    "Email already exists"
                ));

            // 2️⃣ Create USER (auth)
            var user = new User
            {
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "STAFF",
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 3️⃣ Create STAFF (domain)
            var staff = new Staff
            {
                UserId = user.Id,
                staff_code = $"STF{user.Id:D3}",
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Department = dto.Department,
                IsActive = true
            };


            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.Ok("Staff created successfully"));
        }

        // ===============================
        // PUT: api/staff/{id}/disable
        // ===============================
        [HttpPut("{id}/disable")]
        public async Task<IActionResult> Disable(int id)
        {
            var staff = await _context.Staff
                .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
                return NotFound(ApiResponse<string>.Fail(
                    "NOT_FOUND",
                    "Staff not found"
                ));

            // Disable staff record
            staff.IsActive = false;

            // Disable linked user account
            var user = await _context.Users.FindAsync(staff.UserId);
            if (user != null)
                user.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.Ok("Staff disabled"));
        }
    }
}
