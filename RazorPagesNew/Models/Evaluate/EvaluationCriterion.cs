using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Models.Evaluate
{
    public class EvaluationCriterion : BaseEntity
    {
        [Required, StringLength(100)]
        public string Name { get; set; }

        [Range(0, 1)]
        public double Weight { get; set; }

        // Навигационные свойства
        public virtual ICollection<EvaluationScore> Scores { get; set; } = new List<EvaluationScore>();
    }
}
