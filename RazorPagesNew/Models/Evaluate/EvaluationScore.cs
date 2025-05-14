using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Models.Evaluate
{
    public class EvaluationScore : BaseEntity
    {
        public int EvaluationId { get; set; }
        public virtual EmployeeEvaluation Evaluation { get; set; }

        public int CriterionId { get; set; }
        public virtual EvaluationCriterion Criterion { get; set; }

        [Range(0, 100)]
        public double Score { get; set; }
    }
}
