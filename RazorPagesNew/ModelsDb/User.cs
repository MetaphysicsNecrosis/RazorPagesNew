using System;
using System.Collections.Generic;

namespace RazorPagesNew.ModelsDb;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public int? EmployeeId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? IdentityUserId { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<EmployeeEvaluation> EmployeeEvaluationEvaluators { get; set; } = new List<EmployeeEvaluation>();

    public virtual ICollection<EmployeeEvaluation> EmployeeEvaluationOwners { get; set; } = new List<EmployeeEvaluation>();

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<LeaveRecord> LeaveRecords { get; set; } = new List<LeaveRecord>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<TaskRecord> TaskRecords { get; set; } = new List<TaskRecord>();

    public virtual ICollection<WorkActivitySummary> WorkActivitySummaries { get; set; } = new List<WorkActivitySummary>();
}
