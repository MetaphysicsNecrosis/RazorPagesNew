using System;
using System.Collections.Generic;

namespace RazorPagesNew.ModelsDb;

public partial class WorkActivitySummary
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public int TotalLeaveDays { get; set; }

    public int SickDays { get; set; }

    public int VacationDays { get; set; }

    public int AttendanceDays { get; set; }

    public int LateArrivals { get; set; }

    public double TotalHoursWorked { get; set; }

    public int CompletedTasks { get; set; }

    public double AvgTaskEfficiency { get; set; }

    public double AttendanceScore { get; set; }

    public double TaskScore { get; set; }

    public double PenaltyScore { get; set; }

    public int OwnerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual ICollection<EmployeeEvaluation> EmployeeEvaluations { get; set; } = new List<EmployeeEvaluation>();

    public virtual User Owner { get; set; } = null!;
}
