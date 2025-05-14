using RazorPagesNew.Models.Activity;
using RazorPagesNew.Models.Enums;
using RazorPagesNew.Models.Evaluate;
using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Models
{
    public class Employee : OwnedEntity
    {
        [Required, StringLength(100)]
        public string FullName { get; set; }

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; }

        [Phone, StringLength(20)]
        public string Phone { get; set; }

        public int DepartmentId { get; set; }
        public Department Department { get; set; }

        [Required, StringLength(100)]
        public string Position { get; set; }

        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DismissalDate { get; set; }

        [StringLength(255)]
        public string? PhotoPath { get; set; }

        [Range(0, double.MaxValue)]
        public double VacationBalance { get; set; }

        [Range(0, double.MaxValue)]
        public double SickLeaveUsed { get; set; }

        public EmploymentType EmploymentType { get; set; }

        // Навигационные свойства
        public virtual ICollection<EmployeeEvaluation> Evaluations { get; set; } = new List<EmployeeEvaluation>();
        public virtual ICollection<LeaveRecord> LeaveRecords { get; set; } = new List<LeaveRecord>();
        public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
        public virtual ICollection<TaskRecord> Tasks { get; set; } = new List<TaskRecord>();
        public virtual ICollection<WorkActivitySummary> ActivitySummaries { get; set; } = new List<WorkActivitySummary>();
        public virtual User? User { get; set; }
    }
}
