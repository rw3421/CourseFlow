using CourseFlow.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CourseFlow.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // 👇 LOCAL / DESIGN-TIME connection
            var connectionString =
                "Server=localhost;Port=3306;Database=courseflow;User=root;Password=root;";

            optionsBuilder.UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(8, 0, 45))
            );

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
