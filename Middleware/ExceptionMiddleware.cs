using System.Net;
using System.Text.Json;
using CourseFlow.Models.Common;
using Microsoft.Extensions.Hosting;

namespace CourseFlow.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // 🔴 ALWAYS log full exception (server-side only)
                _logger.LogError(ex, "Unhandled exception occurred");

                // 🔍 TEMP: also print to console (helps VS Code debugging)
                Console.WriteLine("🔥 UNHANDLED EXCEPTION 🔥");
                Console.WriteLine(ex.ToString());

                await HandleExceptionAsync(context, _env);
            }
        }

        private static async Task HandleExceptionAsync(
            HttpContext context,
            IHostEnvironment env)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            ApiResponse<string> response;

            if (env.IsDevelopment())
            {
                // ⚠️ DEV MODE: still hide stack trace, but clearer message
                response = ApiResponse<string>.Fail(
                    "SYS-001",
                    "Internal server error (check console logs)"
                );
            }
            else
            {
                // 🔒 PROD MODE: generic message only
                response = ApiResponse<string>.Fail(
                    "SYS-001",
                    "Unexpected error occurred"
                );
            }

            var json = JsonSerializer.Serialize(response);

            await context.Response.WriteAsync(json);
        }
    }
}
