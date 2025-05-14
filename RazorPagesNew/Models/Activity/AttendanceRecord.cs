using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Models.Activity
{
    public class AttendanceRecord : BaseEntity
    {
        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [DataType(DataType.Time)]
        public DateTime CheckIn { get; set; }

        [DataType(DataType.Time)]
        public DateTime CheckOut { get; set; }

        [Range(0, 24)]
        public int HoursWorked { get; set; }

        [StringLength(100)]
        public string? SourceSystem { get; set; }
    }
}
