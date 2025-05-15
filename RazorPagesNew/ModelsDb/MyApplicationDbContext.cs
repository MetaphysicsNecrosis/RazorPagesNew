using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace RazorPagesNew.ModelsDb;

public partial class MyApplicationDbContext : DbContext
{
    public MyApplicationDbContext()
    {
    }

    public MyApplicationDbContext(DbContextOptions<MyApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AttendanceRecord> AttendanceRecords { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EmployeeEvaluation> EmployeeEvaluations { get; set; }

    public virtual DbSet<EvaluationCriterion> EvaluationCriteria { get; set; }

    public virtual DbSet<EvaluationScore> EvaluationScores { get; set; }

    public virtual DbSet<LeaveRecord> LeaveRecords { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<TaskRecord> TaskRecords { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<WorkActivitySummary> WorkActivitySummaries { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-A9QI9P7\\MSSQLSERVER01;Database=EmployeeManagementDB;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attendan__3214EC0765DAC4CD");

            entity.HasIndex(e => e.Date, "IX_AttendanceRecords_Date");

            entity.HasIndex(e => e.EmployeeId, "IX_AttendanceRecords_EmployeeId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.SourceSystem).HasMaxLength(100);

            entity.HasOne(d => d.Employee).WithMany(p => p.AttendanceRecords)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceRecords_Employees");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AuditLog__3214EC0799E845C1");

            entity.HasIndex(e => e.ActionType, "IX_AuditLogs_ActionType");

            entity.HasIndex(e => e.EntityName, "IX_AuditLogs_EntityName");

            entity.HasIndex(e => e.Username, "IX_AuditLogs_Username");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.EntityName).HasMaxLength(100);
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Departme__3214EC07B3C40892");

            entity.HasIndex(e => e.Name, "UQ_Departments_Name").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Employee__3214EC07F03C3BF5");

            entity.HasIndex(e => e.DepartmentId, "IX_Employees_DepartmentId");

            entity.HasIndex(e => e.Email, "IX_Employees_Email");

            entity.HasIndex(e => e.OwnerId, "IX_Employees_OwnerId");

            entity.HasIndex(e => e.Email, "UQ_Employees_Email").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.PhotoPath).HasMaxLength(255);
            entity.Property(e => e.Position).HasMaxLength(100);

            entity.HasOne(d => d.Department).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employees_Departments");

            entity.HasOne(d => d.Owner).WithMany(p => p.Employees)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employees_Users_Owner");
        });

        modelBuilder.Entity<EmployeeEvaluation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Employee__3214EC0701902956");

            entity.HasIndex(e => e.EmployeeId, "IX_EmployeeEvaluations_EmployeeId");

            entity.HasIndex(e => e.EvaluatorId, "IX_EmployeeEvaluations_EvaluatorId");

            entity.HasIndex(e => e.OwnerId, "IX_EmployeeEvaluations_OwnerId");

            entity.HasIndex(e => e.SummaryId, "IX_EmployeeEvaluations_SummaryId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeEvaluations)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeEvaluations_Employees");

            entity.HasOne(d => d.Evaluator).WithMany(p => p.EmployeeEvaluationEvaluators)
                .HasForeignKey(d => d.EvaluatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeEvaluations_Users_Evaluator");

            entity.HasOne(d => d.Owner).WithMany(p => p.EmployeeEvaluationOwners)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeEvaluations_Users_Owner");

            entity.HasOne(d => d.Summary).WithMany(p => p.EmployeeEvaluations)
                .HasForeignKey(d => d.SummaryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeEvaluations_WorkActivitySummaries");
        });

        modelBuilder.Entity<EvaluationCriterion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Evaluati__3214EC074BFC2B6D");

            entity.HasIndex(e => e.Name, "UQ_EvaluationCriteria_Name").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<EvaluationScore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Evaluati__3214EC07B4ACEF76");

            entity.HasIndex(e => e.CriterionId, "IX_EvaluationScores_CriterionId");

            entity.HasIndex(e => e.EvaluationId, "IX_EvaluationScores_EvaluationId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Criterion).WithMany(p => p.EvaluationScores)
                .HasForeignKey(d => d.CriterionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EvaluationScores_EvaluationCriteria");

            entity.HasOne(d => d.Evaluation).WithMany(p => p.EvaluationScores)
                .HasForeignKey(d => d.EvaluationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EvaluationScores_EmployeeEvaluations");
        });

        modelBuilder.Entity<LeaveRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LeaveRec__3214EC0736D5C1A9");

            entity.HasIndex(e => e.EmployeeId, "IX_LeaveRecords_EmployeeId");

            entity.HasIndex(e => e.OwnerId, "IX_LeaveRecords_OwnerId");

            entity.HasIndex(e => new { e.StartDate, e.EndDate }, "IX_LeaveRecords_StartDate_EndDate");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(d => d.Employee).WithMany(p => p.LeaveRecords)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveRecords_Employees");

            entity.HasOne(d => d.Owner).WithMany(p => p.LeaveRecords)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveRecords_Users");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC07A81CD56F");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RefreshTokens_Users");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC07A3BB3B0C");

            entity.HasIndex(e => e.Name, "UQ_Roles_Name").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<TaskRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TaskReco__3214EC074B37AED4");

            entity.HasIndex(e => e.CompletedAt, "IX_TaskRecords_CompletedAt");

            entity.HasIndex(e => e.EmployeeId, "IX_TaskRecords_EmployeeId");

            entity.HasIndex(e => e.OwnerId, "IX_TaskRecords_OwnerId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ExternalSystemId).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Employee).WithMany(p => p.TaskRecords)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskRecords_Employees");

            entity.HasOne(d => d.Owner).WithMany(p => p.TaskRecords)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskRecords_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC0733137B7C");

            entity.HasIndex(e => e.EmployeeId, "IX_Users_EmployeeId");

            entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

            entity.HasIndex(e => e.Username, "IX_Users_Username");

            entity.HasIndex(e => e.Username, "UQ_Users_Username").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IdentityUserId)
                .HasMaxLength(100)
                .IsFixedLength();
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Employee).WithMany(p => p.Users)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Users_Employees");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<WorkActivitySummary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__WorkActi__3214EC0798AB02F0");

            entity.HasIndex(e => e.EmployeeId, "IX_WorkActivitySummaries_EmployeeId");

            entity.HasIndex(e => e.OwnerId, "IX_WorkActivitySummaries_OwnerId");

            entity.HasIndex(e => new { e.PeriodStart, e.PeriodEnd }, "IX_WorkActivitySummaries_PeriodStart_PeriodEnd");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Employee).WithMany(p => p.WorkActivitySummaries)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkActivitySummaries_Employees");

            entity.HasOne(d => d.Owner).WithMany(p => p.WorkActivitySummaries)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkActivitySummaries_Users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
