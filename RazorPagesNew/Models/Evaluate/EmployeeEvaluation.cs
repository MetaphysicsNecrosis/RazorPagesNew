using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Models.Evaluate
{
    public class EmployeeEvaluation : OwnedEntity
    {
        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        public int SummaryId { get; set; }
        public virtual WorkActivitySummary Summary { get; set; }

        public int EvaluatorId { get; set; }
        public virtual User Evaluator { get; set; }

        [DataType(DataType.Date)]
        public DateTime EvaluationDate { get; set; }

        [Range(0, 100)]
        public double Score { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        // Навигационные свойства
        public virtual ICollection<EvaluationScore> Scores { get; set; } = new List<EvaluationScore>();
    }
}
