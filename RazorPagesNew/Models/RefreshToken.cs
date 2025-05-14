using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Models
{
    /// <summary>
    /// Токен обновления для аутентификации
    /// </summary>
    public class RefreshToken : BaseEntity
    {
        [Required]
        public string Token { get; set; }

        public DateTime Expires { get; set; }
        public DateTime? Revoked { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; }

        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= Expires;

        [NotMapped]
        public bool IsActive => Revoked == null && !IsExpired;
    }
}
