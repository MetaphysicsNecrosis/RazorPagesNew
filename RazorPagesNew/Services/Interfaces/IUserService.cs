using RazorPagesNew.ModelsDb;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RazorPagesNew.Services.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Аутентификация пользователя
        /// </summary>
        Task<User> AuthenticateAsync(string username, string password);

        /// <summary>
        /// Получение пользователя по ID
        /// </summary>
        Task<User> GetByIdAsync(string id);

        /// <summary>
        /// Получение пользователя по ID из нашей системы
        /// </summary>
        Task<User> GetByIdAsync(int id);

        /// <summary>
        /// Получение пользователя по имени пользователя
        /// </summary>
        Task<User> GetByUsernameAsync(string username);

        /// <summary>
        /// Создание нового пользователя
        /// </summary>
        Task<bool> CreateUserAsync(User user, string password);

        /// <summary>
        /// Изменение пароля пользователя
        /// </summary>
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        /// <summary>
        /// Проверка наличия роли у пользователя
        /// </summary>
        Task<bool> IsInRoleAsync(int userId, string roleName);

        /// <summary>
        /// Получение всех пользователей с указанной ролью
        /// </summary>
        Task<IEnumerable<User>> GetUsersInRoleAsync(string roleName);

        /// <summary>
        /// Получение всех пользователей
        /// </summary>
        Task<IEnumerable<User>> GetAllUsersAsync();

        /// <summary>
        /// Получение текущего пользователя из ClaimsPrincipal
        /// </summary>
        Task<User> GetCurrentUserAsync(ClaimsPrincipal userPrincipal);

        /// <summary>
        /// Добавление роли пользователю
        /// </summary>
        Task<bool> AddUserToRoleAsync(int userId, string roleName);

        /// <summary>
        /// Удаление роли у пользователя
        /// </summary>
        Task<bool> RemoveUserFromRoleAsync(int userId, string roleName);
    }
}