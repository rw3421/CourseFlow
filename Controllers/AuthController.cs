using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using CourseFlow.Data;
using CourseFlow.Models;
using CourseFlow.Services;

namespace CourseFlow.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;
        private readonly AuditService _auditService;

        public AuthController(
            AppDbContext context,
            TokenService tokenService,
            AuditService auditService)
        {
            _context = context;
            _tokenService = tokenService;
            _auditService = auditService;
        }

        // =====================
        // LOGIN → ISSUE TOKENS
        // =====================
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || user.IsDeleted)
                return Unauthorized("Invalid credentials");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            // 🔐 Revoke existing refresh tokens (single-session policy)
            var existingTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in existingTokens)
            {
                token.IsRevoked = true;
            }

            var accessToken = _tokenService.CreateAccessToken(user);
            var refreshToken = _tokenService.CreateRefreshToken(user.Id);

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            // 📝 Audit log — LOGIN
            await _auditService.LogAsync(
                user.Id,
                "LOGIN",
                "User"
            );

            return Ok(new
            {
                accessToken,
                refreshToken = refreshToken.Token
            });
        }

        // =====================
        // REFRESH TOKEN (ROTATION)
        // =====================
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var storedToken = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == request.RefreshToken);

            if (storedToken == null)
                return Unauthorized("Invalid refresh token");

            if (storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
                return Unauthorized("Refresh token expired or revoked");

            // 🔁 Rotate token
            storedToken.IsRevoked = true;

            var newAccessToken = _tokenService.CreateAccessToken(storedToken.User);
            var newRefreshToken = _tokenService.CreateRefreshToken(storedToken.UserId);

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            // 📝 Audit log — REFRESH
            await _auditService.LogAsync(
                storedToken.UserId,
                "REFRESH",
                "RefreshToken"
            );

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken.Token
            });
        }

        // =====================
        // LOGOUT → REVOKE TOKEN
        // =====================
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
        {
            var token = await _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

            if (token != null)
            {
                token.IsRevoked = true;
                await _context.SaveChangesAsync();

                // 📝 Audit log — LOGOUT
                await _auditService.LogAsync(
                    token.UserId,
                    "LOGOUT",
                    "User"
                );
            }

            return Ok();
        }
    }

    // =====================
    // REQUEST MODELS
    // =====================
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
