using Microsoft.EntityFrameworkCore;
using CourseFlow.Models;
using System;
using System.Linq;


namespace CourseFlow.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }


        // =========================
        // 🔎 GLOBAL QUERY FILTER
        // =========================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // Staff
            // =========================
            modelBuilder.Entity<Staff>(entity =>
            {
                entity.ToTable("staff");

                entity.HasKey(s => s.Id);

                entity.Property(s => s.Id)
                    .HasColumnName("id");

                entity.Property(s => s.staff_code)
                    .HasColumnName("staff_code")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(s => s.FullName)
                    .HasColumnName("full_name")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(s => s.Email)
                    .HasColumnName("email")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(s => s.PhoneNumber)
                    .HasColumnName("phone_number")
                    .HasMaxLength(50);

                entity.Property(s => s.Role)
                    .HasColumnName("role")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(s => s.Department)
                    .HasColumnName("department")
                    .HasMaxLength(100);

                entity.Property(s => s.ProfileImagePath)
                    .HasColumnName("profile_image_path")
                    .HasMaxLength(255);

                entity.Property(s => s.IsActive)
                    .HasColumnName("is_active")
                    .HasDefaultValue(true);

                entity.Property(s => s.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(s => s.UpdatedAt)
                    .HasColumnName("updated_at");

                entity.Property(s => s.UserId)
                    .HasColumnName("user_id");

                entity.HasOne(s => s.User)
                    .WithOne(u => u.Staff)
                    .HasForeignKey<Staff>(s => s.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(s => s.staff_code)
                    .IsUnique();

                entity.HasIndex(s => s.Email)
                    .IsUnique();
            });



            // =========================
            // RefreshToken
            // =========================
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("refresh_tokens");

                entity.HasKey(r => r.Id);

                entity.Property(r => r.Id)
                    .HasColumnName("id");

                entity.Property(r => r.UserId)
                    .HasColumnName("user_id")
                    .IsRequired();

                entity.Property(r => r.Token)
                    .HasColumnName("token")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(r => r.ExpiresAt)
                    .HasColumnName("expires_at")
                    .IsRequired();

                entity.Property(r => r.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(r => r.IsRevoked)
                    .HasColumnName("is_revoked")
                    .HasDefaultValue(false);

                entity.Property(r => r.RevokedAt)
                    .HasColumnName("revoked_at");

                entity.Property(r => r.ReplacedByToken)
                    .HasColumnName("replaced_by_token");

                // 🔥 THIS LINE FIXES UserId1
                entity.HasOne(r => r.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(r => r.Token)
                    .IsUnique();
            });




            // =========================
            // COURSE
            // =========================
            modelBuilder.Entity<Course>(entity =>
            {
                entity.ToTable("courses");

                entity.HasKey(c => c.Id);

                entity.Property(c => c.Id).HasColumnName("id");
                entity.Property(c => c.CourseCode).HasColumnName("course_code");
                entity.Property(c => c.CourseName).HasColumnName("course_name");
                entity.Property(c => c.Description).HasColumnName("description");
                entity.Property(c => c.CreditHours).HasColumnName("credit_hours");

                entity.Property(c => c.staff_id).HasColumnName("staff_id");
                entity.Property(c => c.day_of_week).HasColumnName("day_of_week");
                entity.Property(c => c.StartTime).HasColumnName("start_time");
                entity.Property(c => c.EndTime).HasColumnName("end_time");

                entity.Property(c => c.IsActive).HasColumnName("is_active");
                entity.Property(c => c.CreatedAt).HasColumnName("created_at");
                entity.Property(c => c.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(c => c.Staff)
                      .WithMany()
                      .HasForeignKey(c => c.staff_id)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // =========================
            // COURSE APPROVALS
            // =========================
            modelBuilder.Entity<CourseApproval>(entity =>
            {
                entity.ToTable("course_approvals");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CourseId).HasColumnName("course_id").IsRequired(false);
                entity.Property(e => e.ActionType).HasColumnName("action_type");
                entity.Property(e => e.PayloadJson).HasColumnName("payload_json");

                entity.Property(e => e.RequestedById).HasColumnName("requested_by_id");
                entity.Property(e => e.RequestedByRole).HasColumnName("requested_by_role");

                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.ReviewedById).HasColumnName("reviewed_by_id");
                entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
                entity.Property(e => e.Remarks).HasColumnName("remarks");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            // =========================
            // COURSE ENROLLMENTS
            // =========================
            modelBuilder.Entity<CourseEnrollment>(entity =>
            {
                entity.ToTable("course_enrollments");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.StudentId).HasColumnName("student_id");
                entity.Property(e => e.CourseId).HasColumnName("course_id");

                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.EnrolledAt).HasColumnName("enrolled_at");
                entity.Property(e => e.DroppedAt).HasColumnName("dropped_at");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                // FK → users (student)
                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // FK → courses
                entity.HasOne(e => e.Course)
                    .WithMany()
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                // unique constraint
                entity.HasIndex(e => new { e.StudentId, e.CourseId })
                    .IsUnique();
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("audit_logs");

                entity.Property(a => a.UserId).HasColumnName("UserId");
                entity.Property(a => a.Action).HasColumnName("Action");
                entity.Property(a => a.Entity).HasColumnName("Entity");
                entity.Property(a => a.EntityId).HasColumnName("EntityId");
                entity.Property(a => a.IpAddress).HasColumnName("IpAddress");
                entity.Property(a => a.UserAgent).HasColumnName("UserAgent");
                entity.Property(a => a.CreatedAt).HasColumnName("CreatedAt");
            });



            modelBuilder.Entity<CourseEnrollment>()
                .HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId);

            modelBuilder.Entity<CourseEnrollment>()
                .HasOne(e => e.Course)
                .WithMany()
                .HasForeignKey(e => e.CourseId);


        }

        // =========================
        // AUDIT AUTO-FILL
        // =========================
        public override int SaveChanges()
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is User user)
                {
                    HandleAudit(entry, user, now);
                }
                else if (entry.Entity is UserProfile profile)
                {
                    HandleAudit(entry, profile, now);
                }
            }

            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // 🌏 Sarawak = UTC+8
            var now = DateTime.UtcNow.AddHours(8);

            foreach (var entry in ChangeTracker.Entries())
            {
                // 🔹 Auto timestamps (ALL entities)
                if (entry.State == EntityState.Added)
                {
                    if (entry.Properties.Any(p => p.Metadata.Name == "CreatedAt"))
                    {
                        entry.Property("CreatedAt").CurrentValue = now;
                    }
                }

                if (entry.State == EntityState.Modified)
                {
                    if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                    {
                        entry.Property("UpdatedAt").CurrentValue = now;
                    }
                }

                // 🔹 Your existing audit / soft-delete logic
                if (entry.Entity is User user)
                {
                    HandleAudit(entry, user, now);
                }
                else if (entry.Entity is UserProfile profile)
                {
                    HandleAudit(entry, profile, now);
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }



        private static void HandleAudit(
            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
            dynamic entity,
            DateTime now)
        {
            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Deleted)
            {
                // 🚫 Prevent hard delete
                entry.State = EntityState.Modified;
                entity.IsDeleted = true;
                entity.DeletedAt = now;
            }
        }

        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseApproval> CourseApprovals { get; set; }

        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }

    }
}
