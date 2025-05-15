using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace RazorPagesNew.ModelsDb;

public partial class LeaveRecord
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int Type { get; set; }

    public string? Notes { get; set; }

    public int OwnerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual User Owner { get; set; } = null!;
    // Вычисляемое поле для количества дней отпуска
    [NotMapped]
    public int DayCount => (EndDate - StartDate).Days + 1;
}
