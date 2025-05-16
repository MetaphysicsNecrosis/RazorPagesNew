using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Models.Enums;
using RazorPagesNew.Services.Interfaces;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesNew.Services.Implementation
{
    public class IdentityUserService : IUserService
    {
        private readonly MyApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly UserManager<IdentityUser> _identityUserManager;
        private readonly RoleManager<IdentityRole> _identityRoleManager;
        private readonly IRoleSynchronizationService _roleSyncService;

        public IdentityUserService(
            MyApplicationDbContext context,
            IAuditLogService auditLogService,
            UserManager<IdentityUser> identityUserManager,
            RoleManager<IdentityRole> identityRoleManager,
            IRoleSynchronizationService roleSyncService)
        {
            _context = context;
            _auditLogService = auditLogService;
            _identityUserManager = identityUserManager;
            _identityRoleManager = identityRoleManager;
            _roleSyncService = roleSyncService;
        }

        /// <summary>
        /// Аутентификация пользователя через Identity
        /// </summary>
        public async Task<User> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            // Находим пользователя Identity по имени пользователя
            var identityUser = await _identityUserManager.FindByNameAsync(username);

            // Проверяем пароль
            if (identityUser == null || !await _identityUserManager.CheckPasswordAsync(identityUser, password))
                return null;

            // Находим соответствующего пользователя в нашей системе
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id);

            // Если пользователь не найден в нашей системе, но есть в Identity,
            // синхронизируем данные
            if (user == null)
            {
                // Получаем роли пользователя в Identity
                var roles = await _identityUserManager.GetRolesAsync(identityUser);

                // Берем первую роль или роль по умолчанию
                string roleName = roles.Any() ? roles.First() : "User";

                // Получаем соответствующую роль в нашей системе
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);

                // Если такой роли нет, создаем ее
                if (role == null)
                {
                    role = new Role { Name = roleName };
                    await _context.Roles.AddAsync(role);
                    await _context.SaveChangesAsync();
                }

                // Создаем пользователя в нашей системе
                user = new User
                {
                    Username = username,
                    IdentityUserId = identityUser.Id,
                    RoleId = role.Id,
                    PasswordHash = "MANAGED_BY_IDENTITY" // пароль хранится в Identity
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                // Загружаем роль
                await _context.Entry(user).Reference(u => u.Role).LoadAsync();
            }

            // Логирование успешного входа
            await _auditLogService.LogActivityAsync(username, ActionType.Login, "User", user.Id.ToString(), "Успешный вход в систему");

            return user;
        }

        /// <summary>
        /// Получение пользователя по ID из Identity
        /// </summary>
        public async Task<User> GetByIdAsync(string id)
        {
            // Находим пользователя в нашей системе по IdentityUserId
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.IdentityUserId == id);

            return user;
        }

        /// <summary>
        /// Получение пользователя по ID из нашей системы
        /// </summary>
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
        /// Создание нового пользователя через Identity
        /// </summary>
        public async Task<bool> CreateUserAsync(User user, string password)
        {
            // Проверка на уникальность имени пользователя
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                return false;

            // Создаем пользователя в Identity
            var identityUser = new IdentityUser { UserName = user.Username, Email = user.Username };
            var identityResult = await _identityUserManager.CreateAsync(identityUser, password);

            if (!identityResult.Succeeded)
                return false;

            // Получаем роль
            var role = await _context.Roles.FindAsync(user.RoleId);
            if (role == null)
                return false;

            // Добавляем роль пользователю в Identity
            await _identityUserManager.AddToRoleAsync(identityUser, role.Name);

            // Связываем пользователя Identity с нашей системой
            user.IdentityUserId = identityUser.Id;
            user.PasswordHash = "MANAGED_BY_IDENTITY"; // пароль хранится в Identity

            // Добавляем пользователя в нашу систему
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Логирование создания пользователя
            await _auditLogService.LogActivityAsync("system", ActionType.Create, "User", user.Id.ToString(), $"Создан новый пользователь {user.Username}");

            return true;
        }

        /// <summary>
        /// Изменение пароля пользователя через Identity
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.IdentityUserId))
                return false;

            // Получаем пользователя Identity
            var identityUser = await _identityUserManager.FindByIdAsync(user.IdentityUserId);
            if (identityUser == null)
                return false;

            // Меняем пароль в Identity
            var result = await _identityUserManager.ChangePasswordAsync(identityUser, currentPassword, newPassword);

            if (result.Succeeded)
            {
                // Логирование изменения пароля
                await _auditLogService.LogActivityAsync(user.Username, ActionType.Update, "User", user.Id.ToString(), "Пароль пользователя изменен");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Проверка наличия роли у пользователя через Identity
        /// </summary>
        public async Task<bool> IsInRoleAsync(int userId, string roleName)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.IdentityUserId))
                return false;

            // Получаем пользователя Identity
            var identityUser = await _identityUserManager.FindByIdAsync(user.IdentityUserId);
            if (identityUser == null)
                return false;

            // Проверяем наличие роли в Identity
            return await _identityUserManager.IsInRoleAsync(identityUser, roleName);
        }

        /// <summary>
        /// Получение всех пользователей с указанной ролью через Identity
        /// </summary>
        public async Task<IEnumerable<User>> GetUsersInRoleAsync(string roleName)
        {
            // Получаем пользователей Identity с указанной ролью
            var identityUsers = await _identityUserManager.GetUsersInRoleAsync(roleName);

            // Получаем соответствующих пользователей из нашей системы
            var identityUserIds = identityUsers.Select(u => u.Id).ToList();

            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.IdentityUserId != null && identityUserIds.Contains(u.IdentityUserId))
                .ToListAsync();
        }

        /// <summary>
        /// Получение всех пользователей
        /// </summary>
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
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

            return await GetByIdAsync(userId);
        }

        /// <summary>
        /// Добавление роли пользователю
        /// </summary>
        public async Task<bool> AddUserToRoleAsync(int userId, string roleName)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.IdentityUserId))
                return false;

            await _roleSyncService.AddUserToRoleAsync(user.IdentityUserId, roleName);
            return true;
        }

        /// <summary>
        /// Удаление роли у пользователя
        /// </summary>
        public async Task<bool> RemoveUserFromRoleAsync(int userId, string roleName)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.IdentityUserId))
                return false;

            await _roleSyncService.RemoveUserFromRoleAsync(user.IdentityUserId, roleName);
            return true;
        }

        /*public Task<List<User>> IUserService.GetAllUsersAsync()
        {
            throw new NotImplementedException();
        }*/
    }
}