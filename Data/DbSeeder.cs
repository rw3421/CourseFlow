using CourseFlow.Data;
using CourseFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseFlow.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {

            var adminExists = await db.Users
                .AnyAsync(u => u.Role == "ADMIN" && !u.IsDeleted);

            if (adminExists)
                return;


            var admin = new User
            {
                FullName = "System Admin",
                Email = "admin@courseflow.com",
                Role = "ADMIN",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234")
            };

            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}
