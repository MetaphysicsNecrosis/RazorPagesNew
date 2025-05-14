using RazorPagesNew.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Models.Activity
{
    public class LeaveRecord : OwnedEntity
    {
        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public LeaveType Type { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Вычисляемое поле для количества дней отпуска
        [NotMapped]
        public int DayCount => (EndDate - StartDate).Days + 1;
    }
}
