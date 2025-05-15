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
   /* [Authorize(Roles = "Admin")]*/
    public class RolesModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _identityRoleManager;
        private readonly UserManager<IdentityUser> _identityUserManager;
        private readonly MyApplicationDbContext _dbContext;
        private readonly IRoleSynchronizationService _roleSyncService;

        [TempData]
        public string StatusMessage { get; set; }

        public List<RoleViewModel> Roles { get; set; } = new List<RoleViewModel>();

        public RolesModel(
            RoleManager<IdentityRole> identityRoleManager,
            UserManager<IdentityUser> identityUserManager,
            MyApplicationDbContext dbContext,
            IRoleSynchronizationService roleSyncService)
        {
            _identityRoleManager = identityRoleManager;
            _identityUserManager = identityUserManager;
            _dbContext = dbContext;
            _roleSyncService = roleSyncService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadRolesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateRoleAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                StatusMessage = "������: �������� ���� �� ����� ���� ������";
                await LoadRolesAsync();
                return Page();
            }

            // ���������, ���������� �� ��� ����� ����
            if (await _identityRoleManager.RoleExistsAsync(roleName))
            {
                StatusMessage = "������: ���� � ����� ��������� ��� ����������";
                await LoadRolesAsync();
                return Page();
            }

            // ������� ���� � Identity
            var identityResult = await _identityRoleManager.CreateAsync(new IdentityRole(roleName));

            if (!identityResult.Succeeded)
            {
                var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                StatusMessage = $"������: {errors}";
                await LoadRolesAsync();
                return Page();
            }

            // ������� ���� � ��������� �������
            var customRole = new Role { Name = roleName };
            await _dbContext.Roles.AddAsync(customRole);
            await _dbContext.SaveChangesAsync();

            StatusMessage = "���� ������� �������";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteRoleAsync(string roleId)
        {
            var identityRole = await _identityRoleManager.FindByIdAsync(roleId);
            if (identityRole == null)
            {
                StatusMessage = "������: ���� �� �������";
                return RedirectToPage();
            }

            // ���������, �������� �� ���� ���������
            if (IsSystemRole(identityRole.Name))
            {
                StatusMessage = "������: ���������� ������� ��������� ����";
                return RedirectToPage();
            }

            // ���������, ������������ �� ����
            var usersInRole = await _identityUserManager.GetUsersInRoleAsync(identityRole.Name);
            if (usersInRole.Any())
            {
                StatusMessage = "������: ��� ���� ������������ ��������������. ������� ������� ������������� �� ����.";
                return RedirectToPage();
            }

            // ������� ���� �� Identity
            var identityResult = await _identityRoleManager.DeleteAsync(identityRole);

            if (!identityResult.Succeeded)
            {
                var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                StatusMessage = $"������: {errors}";
                return RedirectToPage();
            }

            // ������� ���� �� ��������� �������
            var customRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == identityRole.Name);
            if (customRole != null)
            {
                _dbContext.Roles.Remove(customRole);
                await _dbContext.SaveChangesAsync();
            }

            StatusMessage = "���� ������� �������";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSyncRolesAsync()
        {
            await _roleSyncService.SynchronizeRolesAsync();
            StatusMessage = "���� ������� ����������������";
            return RedirectToPage();
        }

        private async Task LoadRolesAsync()
        {
            var identityRoles = await _identityRoleManager.Roles.ToListAsync();
            var customRoles = await _dbContext.Roles.ToListAsync();

            Roles.Clear();

            foreach (var identityRole in identityRoles)
            {
                var customRole = customRoles.FirstOrDefault(r => r.Name == identityRole.Name);
                var usersInRole = await _identityUserManager.GetUsersInRoleAsync(identityRole.Name);

                Roles.Add(new RoleViewModel
                {
                    Id = identityRole.Id,
                    Name = identityRole.Name,
                    UserCount = usersInRole.Count,
                    IsInSync = customRole != null,
                    IsSystemRole = IsSystemRole(identityRole.Name)
                });
            }

            // ��������� ����, ������� ���� ������ � ��������� �������
            foreach (var customRole in customRoles.Where(cr => !identityRoles.Any(ir => ir.Name == cr.Name)))
            {
                Roles.Add(new RoleViewModel
                {
                    Id = string.Empty,
                    Name = customRole.Name,
                    UserCount = 0,
                    IsInSync = false,
                    IsSystemRole = IsSystemRole(customRole.Name)
                });
            }

            // ��������� ����: ������� ���������, ����� ����������������
            Roles = Roles.OrderByDescending(r => r.IsSystemRole).ThenBy(r => r.Name).ToList();
        }

        private bool IsSystemRole(string roleName)
        {
            var systemRoles = new[] { "Admin", "Manager", "Evaluator", "Employee", "User" };
            return systemRoles.Contains(roleName);
        }

        public class RoleViewModel
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int UserCount { get; set; }
            public bool IsInSync { get; set; }
            public bool IsSystemRole { get; set; }
        }
    }
}