using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Models
{
    /// <summary>
    /// Модель отдела
    /// </summary>
    public class Department : BaseEntity
    {
        [Required, StringLength(100)]
        public string Name { get; set; }

        // Навигационные свойства
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
