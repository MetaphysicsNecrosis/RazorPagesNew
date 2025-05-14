using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RazorPagesNew.Models;
using RazorPagesNew.Models.Activity;
using RazorPagesNew.Models.Enums;
using RazorPagesNew.Models.Evaluate;

namespace RazorPagesNew.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Models.Employee> Employees { get; set; }
        public DbSet<Models.Department> Departments { get; set; }
        public DbSet<Models.User> Users { get; set; }
        public DbSet<Models.Role> Roles { get; set; }
        public DbSet<Models.RefreshToken> RefreshTokens { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
        public DbSet<LeaveRecord> LeaveRecords { get; set; }
        public DbSet<TaskRecord> TaskRecords { get; set; }
        public DbSet<WorkActivitySummary> WorkActivitySummaries { get; set; }
        public DbSet<EmployeeEvaluation> EmployeeEvaluations { get; set; }
        public DbSet<EvaluationCriterion> EvaluationCriteria { get; set; }
        public DbSet<EvaluationScore> EvaluationScores { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

       /* protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Пользователь и роль
            modelBuilder.Entity<Models.User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Пользователь и сотрудник
            modelBuilder.Entity<Models.User>()
                .HasOne(u => u.Employee)
                .WithOne(e => e.User)
                .HasForeignKey<Models.User>(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Токены и пользователь
            modelBuilder.Entity<Models.RefreshToken>()
                .HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict); // <-- ВАЖНО: убрал каскад

            // Сотрудник и отдел
            modelBuilder.Entity<Models.Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Посещаемость
            modelBuilder.Entity<AttendanceRecord>()
                .HasOne(a => a.Employee)
                .WithMany(e => e.AttendanceRecords)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Отпуска
            modelBuilder.Entity<LeaveRecord>()
                .HasOne(l => l.Employee)
                .WithMany(e => e.LeaveRecords)
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Задачи
            modelBuilder.Entity<TaskRecord>()
                .HasOne(t => t.Employee)
                .WithMany(e => e.Tasks)
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Оценки
            modelBuilder.Entity<EmployeeEvaluation>()
                .HasOne(ee => ee.Employee)
                .WithMany(e => e.Evaluations)
                .HasForeignKey(ee => ee.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeEvaluation>()
                .HasOne(ee => ee.Evaluator)
                .WithMany(u => u.ConductedEvaluations)
                .HasForeignKey(ee => ee.EvaluatorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeEvaluation>()
                .HasOne(ee => ee.Summary)
                .WithMany(was => was.EmployeeEvaluations)
                .HasForeignKey(ee => ee.SummaryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Сводка
            modelBuilder.Entity<WorkActivitySummary>()
                .HasOne(was => was.Employee)
                .WithMany(e => e.ActivitySummaries)
                .HasForeignKey(was => was.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Оценки критериев
            modelBuilder.Entity<EvaluationScore>()
                .HasOne(es => es.Evaluation)
                .WithMany(ee => ee.Scores)
                .HasForeignKey(es => es.EvaluationId)
                .OnDelete(DeleteBehavior.Restrict); // <- тоже убираем каскад

            modelBuilder.Entity<EvaluationScore>()
                .HasOne(es => es.Criterion)
                .WithMany(ec => ec.Scores)
                .HasForeignKey(es => es.CriterionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Аудит
            ApplyGlobalFilters(modelBuilder);
        }

        private void ApplyGlobalFilters(ModelBuilder modelBuilder)
        {
            // Автоматическое применение фильтров по мягкому удалению
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Применение аудита (CreatedAt и UpdatedAt)
                if (typeof(Models.BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType).Property<DateTime>("CreatedAt");
                    modelBuilder.Entity(entityType.ClrType).Property<DateTime?>("UpdatedAt");
                }
            }
        }

        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateAuditFields()
        {
            var now = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries<Models.BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = now;
                }
            }
        }*/
    }
}