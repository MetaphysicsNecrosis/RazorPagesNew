using System.Threading.Tasks;

namespace RazorPagesNew.Services.Interfaces
{
    public interface IRoleSynchronizationService
    {
        /// <summary>
        /// Синхронизирует роли между Identity и кастомной системой
        /// </summary>
        Task SynchronizeRolesAsync();

        /// <summary>
        /// Синхронизирует роли пользователя между Identity и кастомной системой
        /// </summary>
        Task SynchronizeUserRolesAsync(string identityUserId);

        /// <summary>
        /// Добавляет роль пользователю в обеих системах
        /// </summary>
        Task AddUserToRoleAsync(string identityUserId, string roleName);

        /// <summary>
        /// Удаляет роль у пользователя в обеих системах
        /// </summary>
        Task RemoveUserFromRoleAsync(string identityUserId, string roleName);
    }
}