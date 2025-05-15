using RazorPagesNew.Data;
using RazorPagesNew.Models.Enums;
using RazorPagesNew.Models;
using RazorPagesNew.Services.Interfaces;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace RazorPagesNew.Services.Implementation
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public UserService(ApplicationDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Аутентификация пользователя
        /// </summary>
        public async Task<User> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username);

            // Проверяем существование пользователя и правильность пароля
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            // Логирование успешного входа
            await _auditLogService.LogActivityAsync(username, ActionType.Login, "User", user.Id.ToString(), "Успешный вход в систему");

            return user;
        }

        /// <summary>
        /// Получение пользователя по ID
        /// </summary>
        public async Task<User> GetByIdAsync(string id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.IdentityUserId == id);
        }
        public async Task<User> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        /// <summary>
        /// Получение пользователя по имени пользователя
        /// </summary>
        public async Task<User> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        /// <summary>
        /// Создание нового пользователя
        /// </summary>
        public async Task<bool> CreateUserAsync(User user, string password)
        {
            // Проверка на уникальность имени пользователя
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                return false;

            // Хеширование пароля
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // Добавление пользователя
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Логирование создания пользователя
            await _auditLogService.LogActivityAsync("system", ActionType.Create, "User", user.Id.ToString(), $"Создан новый пользователь {user.Username}");

            return true;
        }

        /// <summary>
        /// Изменение пароля пользователя
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // Проверка текущего пароля
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return false;

            // Обновление пароля
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Логирование изменения пароля
            await _auditLogService.LogActivityAsync(user.Username, ActionType.Update, "User", user.Id.ToString(), "Пароль пользователя изменен");

            return true;
        }

        /// <summary>
        /// Проверка наличия роли у пользователя
        /// </summary>
        public async Task<bool> IsInRoleAsync(int userId, string roleName)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user != null && user.Role.Name == roleName;
        }

        /// <summary>
        /// Получение всех пользователей с указанной ролью
        /// </summary>
        public async Task<IEnumerable<User>> GetUsersInRoleAsync(string roleName)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.Name == roleName)
                .ToListAsync();
        }

        /// <summary>
        /// Получение текущего пользователя из ClaimsPrincipal
        /// </summary>
        public async Task<User> GetCurrentUserAsync(ClaimsPrincipal userPrincipal)
        {
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return null;

            return await GetByIdAsync(int.Parse(userId));
        }
    }
}
