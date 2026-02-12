using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CourseFlow.Services;
using CourseFlow.Models.DTOs.Enrollments;
using System.Security.Claims;
using CourseFlow.Models.Common;

namespace CourseFlow.Controllers.Api
{
    [ApiController]
    [Route("api/enrollments")]
    [Authorize(Roles = "STUDENT")]
    public class EnrollmentsController : ControllerBase
    {
        private readonly EnrollmentService _enrollmentService;

        public EnrollmentsController(EnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        [HttpPost]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> Enroll([FromBody] EnrollRequestDto dto)
        {
            int studentId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            await _enrollmentService.EnrollAsync(studentId, dto.CourseId);

            return Ok(ApiResponse<string>.Ok("Enrollment successful"));
        }


    }
}
