using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Models
{
    /// <summary>
    /// Роль пользователя в системе
    /// </summary>
    public class Role : BaseEntity
    {
        [Required, StringLength(50)]
        public string Name { get; set; }

        // Навигационные свойства
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
