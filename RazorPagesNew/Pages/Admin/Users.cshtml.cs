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
                StatusMessage = "������: ������������ �� ������";
                return RedirectToPage();
            }

            // ���������, �� ��������� �� ��������� �������������
            if (await _identityUserManager.IsInRoleAsync(user, "Admin") &&
                !selectedRoles.Contains("Admin"))
            {
                var adminUsers = await _identityUserManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count <= 1)
                {
                    StatusMessage = "������: ���������� ������� ���� Admin � ���������� ��������������";
                    return RedirectToPage();
                }
            }

            // �������� ������� ���� ������������
            var userRoles = await _identityUserManager.GetRolesAsync(user);

            // ������� ������������ �� �����, ������� ��� � selectedRoles
            var rolesToRemove = userRoles.Where(r => !selectedRoles.Contains(r)).ToList();
            foreach (var role in rolesToRemove)
            {
                await _roleSyncService.RemoveUserFromRoleAsync(userId, role);
            }

            // ��������� ������������ � ���� �� selectedRoles, � ������� ��� ��� ���
            var rolesToAdd = selectedRoles.Where(r => !userRoles.Contains(r)).ToList();
            foreach (var role in rolesToAdd)
            {
                await _roleSyncService.AddUserToRoleAsync(userId, role);
            }

            StatusMessage = $"���� ������������ {user.UserName} ������� ���������";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
        {
            var user = await _identityUserManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "������: ������������ �� ������";
                return RedirectToPage();
            }

            // ���������, �� ��������� �� ��������� �������������
            if (await _identityUserManager.IsInRoleAsync(user, "Admin"))
            {
                var adminUsers = await _identityUserManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count <= 1)
                {
                    StatusMessage = "������: ���������� ������� ���������� ��������������";
                    return RedirectToPage();
                }
            }

            // �������� ������������ �� ��������� �������
            var customUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.IdentityUserId == userId);

            // ������� ������������ �� ��������� �������
            if (customUser != null)
            {
                _dbContext.Users.Remove(customUser);
                await _dbContext.SaveChangesAsync();
            }

            // ������� ������������ �� Identity
            var result = await _identityUserManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                StatusMessage = $"������: {errors}";
                return RedirectToPage();
            }

            StatusMessage = $"������������ {user.UserName} ������� ������";
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
                    // �������� ���� ������������ � Identity
                    var roles = await _identityUserManager.GetRolesAsync(identityUser);

                    // ����� ������ ���� ��� ���� �� ���������
                    string roleName = roles.Any() ? roles.First() : "User";

                    // �������� ��������������� ���� � ����� �������
                    var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName);

                    // ���� ����� ���� ���, ������� ��
                    if (role == null)
                    {
                        role = new Role { Name = roleName };
                        await _dbContext.Roles.AddAsync(role);
                        await _dbContext.SaveChangesAsync();
                    }

                    // ������� ������������ � ����� �������
                    customUser = new User
                    {
                        Username = identityUser.UserName,
                        IdentityUserId = identityUser.Id,
                        RoleId = role.Id,
                        PasswordHash = "MANAGED_BY_IDENTITY" // ������ �������� � Identity
                    };

                    await _dbContext.Users.AddAsync(customUser);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    // �������������� ���� ������������
                    await _roleSyncService.SynchronizeUserRolesAsync(identityUser.Id);
                }
            }

            StatusMessage = "������������ ������� ����������������";
            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            // ��������� ��� ����
            AllRoles = await _identityRoleManager.Roles.Select(r => r.Name).ToListAsync();

            // ��������� �������������
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

            // ��������� �������������: ������� ��������������, ����� �� �����
            Users = Users
                .OrderByDescending(u => u.Roles.Contains("Admin"))
                .ThenBy(u => u.UserName)
                .ToList();
        }

        private bool IsSystemUser(string userName)
        {
            // ���������, �������� �� ������������ ��������� (��������, admin@example.com)
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