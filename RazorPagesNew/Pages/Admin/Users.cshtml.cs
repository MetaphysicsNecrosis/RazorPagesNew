using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Admin
{
    /*[Authorize(Roles = "Admin")]*/
    public class UsersModel : PageModel
    {
        private readonly UserManager<IdentityUser> _identityUserManager;
        private readonly RoleManager<IdentityRole> _identityRoleManager;
        private readonly MyApplicationDbContext _dbContext;
        private readonly IRoleSynchronizationService _roleSyncService;

        [TempData]
        public string StatusMessage { get; set; }

        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
        public List<string> AllRoles { get; set; } = new List<string>();
        public List<UserRolesJson> UsersRolesJson { get; set; } = new List<UserRolesJson>();

        public UsersModel(
            UserManager<IdentityUser> identityUserManager,
            RoleManager<IdentityRole> identityRoleManager,
            MyApplicationDbContext dbContext,
            IRoleSynchronizationService roleSyncService)
        {
            _identityUserManager = identityUserManager;
            _identityRoleManager = identityRoleManager;
            _dbContext = dbContext;
            _roleSyncService = roleSyncService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateRolesAsync(string userId, List<string> selectedRoles)
        {
            var user = await _identityUserManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "Ошибка: Пользователь не найден";
                return RedirectToPage();
            }

            // Проверяем, не удаляется ли последний администратор
            if (await _identityUserManager.IsInRoleAsync(user, "Admin") &&
                !selectedRoles.Contains("Admin"))
            {
                var adminUsers = await _identityUserManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count <= 1)
                {
                    StatusMessage = "Ошибка: Невозможно удалить роль Admin у последнего администратора";
                    return RedirectToPage();
                }
            }

            // Получаем текущие роли пользователя
            var userRoles = await _identityUserManager.GetRolesAsync(user);

            // Удаляем пользователя из ролей, которых нет в selectedRoles
            var rolesToRemove = userRoles.Where(r => !selectedRoles.Contains(r)).ToList();
            foreach (var role in rolesToRemove)
            {
                await _roleSyncService.RemoveUserFromRoleAsync(userId, role);
            }

            // Добавляем пользователя в роли из selectedRoles, в которых его еще нет
            var rolesToAdd = selectedRoles.Where(r => !userRoles.Contains(r)).ToList();
            foreach (var role in rolesToAdd)
            {
                await _roleSyncService.AddUserToRoleAsync(userId, role);
            }

            StatusMessage = $"Роли пользователя {user.UserName} успешно обновлены";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
        {
            var user = await _identityUserManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "Ошибка: Пользователь не найден";
                return RedirectToPage();
            }

            // Проверяем, не удаляется ли последний администратор
            if (await _identityUserManager.IsInRoleAsync(user, "Admin"))
            {
                var adminUsers = await _identityUserManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count <= 1)
                {
                    StatusMessage = "Ошибка: Невозможно удалить последнего администратора";
                    return RedirectToPage();
                }
            }

            // Получаем пользователя из кастомной системы
            var customUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.IdentityUserId == userId);

            // Удаляем пользователя из кастомной системы
            if (customUser != null)
            {
                _dbContext.Users.Remove(customUser);
                await _dbContext.SaveChangesAsync();
            }

            // Удаляем пользователя из Identity
            var result = await _identityUserManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                StatusMessage = $"Ошибка: {errors}";
                return RedirectToPage();
            }

            StatusMessage = $"Пользователь {user.UserName} успешно удален";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSyncUsersAsync()
        {
            var identityUsers = await _identityUserManager.Users.ToListAsync();
            var customUsers = await _dbContext.Users
                .Include(u => u.Role)
                .ToListAsync();

            foreach (var identityUser in identityUsers)
            {
                var customUser = customUsers.FirstOrDefault(u => u.IdentityUserId == identityUser.Id);
                if (customUser == null)
                {
                    // Получаем роли пользователя в Identity
                    var roles = await _identityUserManager.GetRolesAsync(identityUser);

                    // Берем первую роль или роль по умолчанию
                    string roleName = roles.Any() ? roles.First() : "User";

                    // Получаем соответствующую роль в нашей системе
                    var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName);

                    // Если такой роли нет, создаем ее
                    if (role == null)
                    {
                        role = new Role { Name = roleName };
                        await _dbContext.Roles.AddAsync(role);
                        await _dbContext.SaveChangesAsync();
                    }

                    // Создаем пользователя в нашей системе
                    customUser = new User
                    {
                        Username = identityUser.UserName,
                        IdentityUserId = identityUser.Id,
                        RoleId = role.Id,
                        PasswordHash = "MANAGED_BY_IDENTITY" // пароль хранится в Identity
                    };

                    await _dbContext.Users.AddAsync(customUser);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    // Синхронизируем роли пользователя
                    await _roleSyncService.SynchronizeUserRolesAsync(identityUser.Id);
                }
            }

            StatusMessage = "Пользователи успешно синхронизированы";
            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            // Загружаем все роли
            AllRoles = await _identityRoleManager.Roles.Select(r => r.Name).ToListAsync();

            // Загружаем пользователей
            var identityUsers = await _identityUserManager.Users.ToListAsync();
            var customUsers = await _dbContext.Users
                .Include(u => u.Role)
                .ToListAsync();

            Users.Clear();
            UsersRolesJson.Clear();

            foreach (var identityUser in identityUsers)
            {
                var customUser = customUsers.FirstOrDefault(u => u.IdentityUserId == identityUser.Id);
                var userRoles = await _identityUserManager.GetRolesAsync(identityUser);

                var userViewModel = new UserViewModel
                {
                    Id = identityUser.Id,
                    UserName = identityUser.UserName,
                    Email = identityUser.Email,
                    Roles = userRoles.ToList(),
                    IsInSync = customUser != null,
                    IsSystemUser = IsSystemUser(identityUser.UserName)
                };

                Users.Add(userViewModel);

                UsersRolesJson.Add(new UserRolesJson
                {
                    Id = identityUser.Id,
                    Roles = userRoles.ToList()
                });
            }

            // Сортируем пользователей: сначала администраторы, потом по имени
            Users = Users
                .OrderByDescending(u => u.Roles.Contains("Admin"))
                .ThenBy(u => u.UserName)
                .ToList();
        }

        private bool IsSystemUser(string userName)
        {
            // Проверяем, является ли пользователь системным (например, admin@example.com)
            return userName.Equals("admin@example.com", System.StringComparison.OrdinalIgnoreCase);
        }

        public class UserViewModel
        {
            public string Id { get; set; }
            public string UserName { get; set; }
            public string Email { get; set; }
            public List<string> Roles { get; set; } = new List<string>();
            public bool IsInSync { get; set; }
            public bool IsSystemUser { get; set; }
        }

        public class UserRolesJson
        {
            public string Id { get; set; }
            public List<string> Roles { get; set; } = new List<string>();
        }
    }
}