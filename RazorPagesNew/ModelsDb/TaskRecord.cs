using System;
using System.Collections.Generic;

namespace RazorPagesNew.ModelsDb;

public partial class TaskRecord
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CompletedAt { get; set; }

    public string? ExternalSystemId { get; set; }

    public double? EfficiencyScore { get; set; }

    public int Importance { get; set; }

    public int OwnerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual User Owner { get; set; } = null!;
}
