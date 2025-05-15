using System;
using System.Collections.Generic;

namespace RazorPagesNew.ModelsDb;

public partial class Employee
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public int DepartmentId { get; set; }

    public string Position { get; set; } = null!;

    public DateTime HireDate { get; set; }

    public DateTime? DismissalDate { get; set; }

    public string? PhotoPath { get; set; }

    public double VacationBalance { get; set; }

    public double SickLeaveUsed { get; set; }

    public int EmploymentType { get; set; }

    public int OwnerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<EmployeeEvaluation> EmployeeEvaluations { get; set; } = new List<EmployeeEvaluation>();

    public virtual ICollection<LeaveRecord> LeaveRecords { get; set; } = new List<LeaveRecord>();

    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<TaskRecord> TaskRecords { get; set; } = new List<TaskRecord>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<WorkActivitySummary> WorkActivitySummaries { get; set; } = new List<WorkActivitySummary>();
}
