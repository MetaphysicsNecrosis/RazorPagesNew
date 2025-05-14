using RazorPagesNew.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Models
{
    /// <summary>
    /// Запись аудита действий пользователей
    /// </summary>
    public class AuditLog : BaseEntity
    {
        [Required, StringLength(50)]
        public string Username { get; set; }

        public ActionType ActionType { get; set; }

        [Required, StringLength(100)]
        public string EntityName { get; set; }

        [Required]
        public string EntityId { get; set; }

        [Required]
        public string Details { get; set; }
    }
}
