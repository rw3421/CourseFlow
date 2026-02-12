using CourseFlow.Data;
using CourseFlow.Middleware;
using CourseFlow.Services;
using CourseFlow.Models.Common;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =====================
// CONFIGURATION
// =====================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is missing");

var jwtConfig = builder.Configuration.GetSection("Jwt");

// =====================
// SERVICES
// =====================

// Database (Docker-safe)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 45)),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 10,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null
            );
        });
});

// Razor Pages (UI)
builder.Services.AddRazorPages();

// Controllers (API)
builder.Services.AddControllers();

// =====================
// AUTHENTICATION (HYBRID)
// =====================
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "SmartAuth";
        options.DefaultChallengeScheme = "SmartAuth";
    })
    .AddPolicyScheme("SmartAuth", "JWT or Cookie", options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Path.StartsWithSegments("/api")
                ? JwtBearerDefaults.AuthenticationScheme
                : CookieAuthenticationDefaults.AuthenticationScheme;
    })

    // JWT (API)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig["Issuer"],
            ValidAudience = jwtConfig["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtConfig["Key"]!)
            )
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                if (!context.Request.Path.StartsWithSegments("/api"))
                    return;

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(
                    ApiResponse<string>.Fail("UNAUTHORIZED", "Authentication is required")
                );
            },

            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(
                    ApiResponse<string>.Fail("FORBIDDEN", "You do not have permission to perform this action")
                );
            }
        };
    })

    // Cookie (UI)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";

        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = false;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;

        options.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
    });

// =====================
// AUTHORIZATION
// =====================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageStudents", policy =>
        policy.RequireRole("ADMIN"));
});

// =====================
// APP SERVICES
// =====================
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// =====================
// DATABASE MIGRATION (SAFE)
// =====================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    for (int i = 0; i < 10; i++)
    {
        try
        {
            db.Database.Migrate();
            DbSeeder.SeedAsync(db).Wait();
            break;
        }
        catch (MySqlConnector.MySqlException)
        {
            Thread.Sleep(3000);
        }
    }
}

// =====================
// MIDDLEWARE PIPELINE
// =====================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api"),
    apiApp => apiApp.UseMiddleware<ExceptionMiddleware>()
);

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
