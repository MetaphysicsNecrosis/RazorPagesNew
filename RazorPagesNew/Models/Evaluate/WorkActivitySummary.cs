using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Models.Evaluate
{
    public class WorkActivitySummary : OwnedEntity
    {
        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        [DataType(DataType.Date)]
        public DateTime PeriodStart { get; set; }

        [DataType(DataType.Date)]
        public DateTime PeriodEnd { get; set; }

        // Отпуска и больничные
        [Range(0, int.MaxValue)]
        public int TotalLeaveDays { get; set; }

        [Range(0, int.MaxValue)]
        public int SickDays { get; set; }

        [Range(0, int.MaxValue)]
        public int VacationDays { get; set; }

        // Посещаемость
        [Range(0, int.MaxValue)]
        public int AttendanceDays { get; set; }

        [Range(0, int.MaxValue)]
        public int LateArrivals { get; set; }

        [Range(0, double.MaxValue)]
        public double TotalHoursWorked { get; set; }

        // Работа
        [Range(0, int.MaxValue)]
        public int CompletedTasks { get; set; }

        [Range(0, 100)]
        public double AvgTaskEfficiency { get; set; }

        // Оценочные показатели
        [Range(0, 100)]
        public double AttendanceScore { get; set; }

        [Range(0, 100)]
        public double TaskScore { get; set; }

        [Range(0, 100)]
        public double PenaltyScore { get; set; }

        // Навигационные свойства
        public virtual ICollection<EmployeeEvaluation> EmployeeEvaluations { get; set; } = new List<EmployeeEvaluation>();
    }
}
