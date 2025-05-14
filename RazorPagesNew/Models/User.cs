using RazorPagesNew.Models.Evaluate;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace RazorPagesNew.Models
{
    /// <summary>
    /// Модель пользователя системы
    /// </summary>
    public class User : BaseEntity
    {
        [Required, StringLength(50)]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public int RoleId { get; set; }
        public virtual Role Role { get; set; }

        public int? EmployeeId { get; set; }
        public virtual Employee? Employee { get; set; }

        // Навигационные свойства
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public virtual ICollection<EmployeeEvaluation> ConductedEvaluations { get; set; } = new List<EmployeeEvaluation>();
        public virtual ICollection<OwnedEntity> OwnedEntities { get; set; } = new List<OwnedEntity>();
    }
}
