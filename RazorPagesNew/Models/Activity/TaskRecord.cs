using RazorPagesNew.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Models.Activity
{
    public class TaskRecord : OwnedEntity
    {
        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTime CompletedAt { get; set; }

        [StringLength(100)]
        public string? ExternalSystemId { get; set; }

        [Range(0, 100)]
        public double? EfficiencyScore { get; set; }

        public TaskImportance Importance { get; set; }
    }
}
