using System;
using System.Collections.Generic;

namespace RazorPagesNew.ModelsDb;

public partial class EmployeeEvaluation
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public int SummaryId { get; set; }

    public int EvaluatorId { get; set; }

    public DateTime EvaluationDate { get; set; }

    public double Score { get; set; }

    public string Notes { get; set; } = null!;

    public int OwnerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual ICollection<EvaluationScore> EvaluationScores { get; set; } = new List<EvaluationScore>();

    public virtual User Evaluator { get; set; } = null!;

    public virtual User Owner { get; set; } = null!;

    public virtual WorkActivitySummary Summary { get; set; } = null!;
}
