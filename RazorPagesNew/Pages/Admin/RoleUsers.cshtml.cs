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
    public class RoleUsersModel : PageModel
    {
        private readonly UserManager<IdentityUser> _identityUserManager;
        private readonly IRoleSynchronizationService _roleSyncService;
        private readonly MyApplicationDbContext _dbContext;

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public string RoleName { get; set; }

        public List<UserViewModel> UsersInRole { get; set; } = new List<UserViewModel>();
        public List<UserViewModel> UsersNotInRole { get; set; } = new List<UserViewModel>();

        public RoleUsersModel(
            UserManager<IdentityUser> identityUserManager,
            IRoleSynchronizationService roleSyncService,
            MyApplicationDbContext dbContext)
        {
            _identityUserManager = identityUserManager;
            _roleSyncService = roleSyncService;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> OnGetAsync(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                return RedirectToPage("./Roles");
            }

            RoleName = roleName;
            await LoadUsersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddToRoleAsync(string userId, string roleName)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roleName))
            {
                StatusMessage = "Ошибка: Неверные параметры";
                return RedirectToPage(new { roleName });
            }

            var user = await _identityUserManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "Ошибка: Пользователь не найден";
                return RedirectToPage(new { roleName });
            }

            // Проверяем, не находится ли пользователь уже в этой роли
            if (await _identityUserManager.IsInRoleAsync(user, roleName))
            {
                StatusMessage = "Ошибка: Пользователь уже находится в этой роли";
                return RedirectToPage(new { roleName });
            }

            // Синхронизируем добавление роли
            await _roleSyncService.AddUserToRoleAsync(userId, roleName);
            StatusMessage = $"Пользователь {user.UserName} успешно добавлен в роль {roleName}";
            return RedirectToPage(new { roleName });
        }

        public async Task<IActionResult> OnPostRemoveFromRoleAsync(string userId, string roleName)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roleName))
            {
                StatusMessage = "Ошибка: Неверные параметры";
                return RedirectToPage(new { roleName });
            }

            var user = await _identityUserManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "Ошибка: Пользователь не найден";
                return RedirectToPage(new { roleName });
            }

            // Проверяем, находится ли пользователь в этой роли
            if (!await _identityUserManager.IsInRoleAsync(user, roleName))
            {
                StatusMessage = "Ошибка: Пользователь не находится в этой роли";
                return RedirectToPage(new { roleName });
            }

            // Если роль Admin и это последний администратор, запрещаем удаление
            if (roleName == "Admin")
            {
                var adminUsers = await _identityUserManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count <= 1)
                {
                    StatusMessage = "Ошибка: Невозможно удалить последнего администратора";
                    return RedirectToPage(new { roleName });
                }
            }

            // Синхронизируем удаление роли
            await _roleSyncService.RemoveUserFromRoleAsync(userId, roleName);
            StatusMessage = $"Пользователь {user.UserName} успешно удален из роли {roleName}";
            return RedirectToPage(new { roleName });
        }

        private async Task LoadUsersAsync()
        {
            // Получаем пользователей в роли
            var usersInRole = await _identityUserManager.GetUsersInRoleAsync(RoleName);
            var customUsers = await _dbContext.Users.ToListAsync();

            UsersInRole.Clear();
            foreach (var user in usersInRole)
            {
                var customUser = customUsers.FirstOrDefault(u => u.IdentityUserId == user.Id);

                UsersInRole.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    IsInSync = customUser != null && customUser.Role.Name == RoleName
                });
            }

            // Получаем пользователей не в роли
            var allUsers = await _identityUserManager.Users.ToListAsync();
            var usersNotInRole = allUsers.Where(u => !usersInRole.Any(ur => ur.Id == u.Id)).ToList();

            UsersNotInRole.Clear();
            foreach (var user in usersNotInRole)
            {
                UsersNotInRole.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email
                });
            }
        }

        public class UserViewModel
        {
            public string Id { get; set; }
            public string UserName { get; set; }
            public string Email { get; set; }
            public bool IsInSync { get; set; }
        }
    }
}