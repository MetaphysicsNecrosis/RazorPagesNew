using System;
using System.Collections.Generic;

namespace RazorPagesNew.ModelsDb;

public partial class EvaluationCriterion
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public double Weight { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<EvaluationScore> EvaluationScores { get; set; } = new List<EvaluationScore>();
}
