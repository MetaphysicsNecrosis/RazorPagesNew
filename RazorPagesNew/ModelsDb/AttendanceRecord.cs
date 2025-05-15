using System;
using System.Collections.Generic;

namespace RazorPagesNew.ModelsDb;

public partial class AttendanceRecord
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public DateTime Date { get; set; }

    public DateTime CheckIn { get; set; }

    public DateTime CheckOut { get; set; }

    public int HoursWorked { get; set; }

    public string? SourceSystem { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
