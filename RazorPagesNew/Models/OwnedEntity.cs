namespace RazorPagesNew.Models
{
    /// <summary>
    /// Базовый класс для сущностей, которые требуют отслеживания владельца
    /// </summary>
    public abstract class OwnedEntity : BaseEntity
    {
        public int OwnerId { get; set; }
        public User Owner { get; set; }
    }
}
