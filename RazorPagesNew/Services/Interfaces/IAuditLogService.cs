using RazorPagesNew.Models.Enums;
using RazorPagesNew.ModelsDb;

namespace RazorPagesNew.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogActivityAsync(string username, ActionType actionType, string entityName, string entityId, string details);
        Task<IEnumerable<AuditLog>> GetUserActivityLogAsync(string username, int limit = 100);
        Task<IEnumerable<AuditLog>> GetEntityActivityLogAsync(string entityName, string entityId);
        Task<IEnumerable<AuditLog>> GetRecentActivityLogAsync(int limit = 100);
    }
}
