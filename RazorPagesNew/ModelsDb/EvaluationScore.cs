using System;
using System.Collections.Generic;

namespace RazorPagesNew.ModelsDb;

public partial class EvaluationScore
{
    public int Id { get; set; }

    public int EvaluationId { get; set; }

    public int CriterionId { get; set; }

    public double Score { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual EvaluationCriterion Criterion { get; set; } = null!;

    public virtual EmployeeEvaluation Evaluation { get; set; } = null!;
}
