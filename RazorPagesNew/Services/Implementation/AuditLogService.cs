using RazorPagesNew.Data;

using RazorPagesNew.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Models.Enums;

namespace RazorPagesNew.Services.Implementation
{
    public class AuditLogService : IAuditLogService
    {
        private readonly MyApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(MyApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Логирование активности пользователя
        /// </summary>
        public async Task LogActivityAsync(string username, ActionType actionType, string entityName, string entityId, string details)
        {
            var log = new AuditLog
            {
                Username = username,
                ActionType = ((int)actionType),
                EntityName = entityName,
                EntityId = entityId,
                Details = details
            };

            await _context.AuditLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Получение журнала активности конкретного пользователя
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetUserActivityLogAsync(string username, int limit = 100)
        {
            return await _context.AuditLogs
                .Where(l => l.Username == username)
                .OrderByDescending(l => l.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Получение журнала активности для конкретной сущности
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetEntityActivityLogAsync(string entityName, string entityId)
        {
            return await _context.AuditLogs
                .Where(l => l.EntityName == entityName && l.EntityId == entityId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Получение последних записей журнала активности
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetRecentActivityLogAsync(int limit = 100)
        {
            return await _context.AuditLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
    }
}
