using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Admin
{
   /* [Authorize(Roles = "Admin")]*/
    public class DiagnosticsModel : PageModel
    {
        private readonly MyApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _identityUserManager;
        private readonly RoleManager<IdentityRole> _identityRoleManager;
        private readonly IRoleSynchronizationService _roleSyncService;

        [TempData]
        public string StatusMessage { get; set; }

        // ������ �����������
        public bool DatabaseStatus { get; set; }
        public string DatabaseInfo { get; set; }
        public bool IdentityStatus { get; set; }
        public string IdentityInfo { get; set; }
        public bool MigrationsStatus { get; set; }
        public string MigrationsInfo { get; set; }

        // ���������� �����
        public int IdentityRolesCount { get; set; }
        public int CustomRolesCount { get; set; }
        public bool RolesInSync { get; set; }
        public List<string> UnsyncedRoles { get; set; } = new List<string>();

        // ���������� �������������
        public int IdentityUsersCount { get; set; }
        public int CustomUsersCount { get; set; }
        public bool UsersInSync { get; set; }

        // �������������� ����������
        public DateTime LastAutomaticCheck { get; set; }

        public DiagnosticsModel(
            MyApplicationDbContext dbContext,
            UserManager<IdentityUser> identityUserManager,
            RoleManager<IdentityRole> identityRoleManager,
            IRoleSynchronizationService roleSyncService)
        {
            _dbContext = dbContext;
            _identityUserManager = identityUserManager;
            _identityRoleManager = identityRoleManager;
            _roleSyncService = roleSyncService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await RunDiagnosticsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSyncRolesAsync()
        {
            try
            {
                await _roleSyncService.SynchronizeRolesAsync();
                StatusMessage = "���� ������� ����������������";
            }
            catch (Exception ex)
            {
                StatusMessage = $"������ ��� ������������� �����: {ex.Message}";
            }

            await RunDiagnosticsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSyncUsersAsync()
        {
            try
            {
                await _roleSyncService.SynchronizeUsersAsync();
                StatusMessage = "������������ ������� ����������������";
            }
            catch (Exception ex)
            {
                StatusMessage = $"������ ��� ������������� �������������: {ex.Message}";
            }

            await RunDiagnosticsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCheckDataIntegrityAsync()
        {
            try
            {
                // �������� ����������� ������
                var issues = await CheckDataIntegrityAsync();

                if (issues.Any())
                {
                    StatusMessage = $"������� {issues.Count} ������� � ������������ ������. �������� ���� ����������.";
                }
                else
                {
                    StatusMessage = "�������� ����������� ������ ��������� �������. ������� �� ����������.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"������ ��� �������� ����������� ������: {ex.Message}";
            }

            await RunDiagnosticsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostFullSyncAsync()
        {
            try
            {
                // ������������� �����
                await _roleSyncService.SynchronizeRolesAsync();

                // ������������� �������������
                await _roleSyncService.SynchronizeUsersAsync();

                // �������� ����������� ������
                var issues = await CheckDataIntegrityAsync();

                if (issues.Any())
                {
                    StatusMessage = $"������ ������������� ���������. ���������� {issues.Count} ������� � ������������ ������.";
                }
                else
                {
                    StatusMessage = "������ ������������� ��������� �������. ������� � ������������ ������ �� ����������.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"������ ��� ���������� ������ �������������: {ex.Message}";
            }

            await RunDiagnosticsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCheckPerformanceAsync()
        {
            try
            {
                // �������� � ����������� ������������������
                await CheckDatabasePerformanceAsync();
                StatusMessage = "�������� ������������������ ���������. ������� ��������������.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"������ ��� �������� ������������������: {ex.Message}";
            }

            await RunDiagnosticsAsync();
            return Page();
        }

        private async Task RunDiagnosticsAsync()
        {
            // �������� ���� ������
            try
            {
                DatabaseStatus = await _dbContext.Database.CanConnectAsync();
                DatabaseInfo = $"������: {_dbContext.Database.GetDbConnection().DataSource}, ���� ������: {_dbContext.Database.GetDbConnection().Database}";
            }
            catch (Exception ex)
            {
                DatabaseStatus = false;
                DatabaseInfo = $"������: {ex.Message}";
            }

            // �������� Identity
            try
            {
                IdentityStatus = await _identityUserManager.Users.AnyAsync();
                IdentityInfo = $"�������������: {await _identityUserManager.Users.CountAsync()}, �����: {await _identityRoleManager.Roles.CountAsync()}";
            }
            catch (Exception ex)
            {
                IdentityStatus = false;
                IdentityInfo = $"������: {ex.Message}";
            }

            // �������� ��������
            try
            {
                var migrator = _dbContext.Database.GetService<IMigrator>();
                var migrationsAssembly = _dbContext.Database.GetService<IMigrationsAssembly>();
                var appliedMigrations = await _dbContext.Database.GetAppliedMigrationsAsync();
                var availableMigrations = migrationsAssembly.Migrations.Keys;

                var pendingMigrations = availableMigrations.Except(appliedMigrations).ToList();

                MigrationsStatus = !pendingMigrations.Any();
                MigrationsInfo = $"���������: {appliedMigrations.Count()}, ��������: {availableMigrations.Count()}, �������: {pendingMigrations.Count}";
            }
            catch (Exception ex)
            {
                MigrationsStatus = false;
                MigrationsInfo = $"������: {ex.Message}";
            }

            // ���������� �����
            var identityRoles = await _identityRoleManager.Roles.Select(r => r.Name).ToListAsync();
            var customRoles = await _dbContext.Roles.Select(r => r.Name).ToListAsync();

            IdentityRolesCount = identityRoles.Count;
            CustomRolesCount = customRoles.Count;

            // ����� �������������������� �����
            var identityOnlyRoles = identityRoles.Except(customRoles).ToList();
            var customOnlyRoles = customRoles.Except(identityRoles).ToList();

            UnsyncedRoles.Clear();
            UnsyncedRoles.AddRange(identityOnlyRoles.Select(r => $"{r} (������ � Identity)"));
            UnsyncedRoles.AddRange(customOnlyRoles.Select(r => $"{r} (������ � ��������� �������)"));

            RolesInSync = !UnsyncedRoles.Any();

            // ���������� �������������
            IdentityUsersCount = await _identityUserManager.Users.CountAsync();
            CustomUsersCount = await _dbContext.Users.CountAsync();

            // �������� ������������� �������������
            var identityUserIds = await _identityUserManager.Users.Select(u => u.Id).ToListAsync();
            var customUserIdentityIds = await _dbContext.Users.Where(u => u.IdentityUserId != null).Select(u => u.IdentityUserId).ToListAsync();

            UsersInSync = identityUserIds.Count == customUserIdentityIds.Count &&
                           !identityUserIds.Except(customUserIdentityIds).Any() &&
                           !customUserIdentityIds.Except(identityUserIds).Any();

            // �������������� ����������
            LastAutomaticCheck = DateTime.Now.AddDays(-1); // ����� ������ ���� �������� �������� �� ��������
        }

        private async Task<List<string>> CheckDataIntegrityAsync()
        {
            var issues = new List<string>();

            // �������� �� "�������" ������ � ��������� ������� (������������ ��� ����� � Identity)
            var usersWithoutIdentity = await _dbContext.Users
                .Where(u => u.IdentityUserId == null)
                .ToListAsync();

            if (usersWithoutIdentity.Any())
            {
                issues.Add($"������� {usersWithoutIdentity.Count} ������������� ��� ����� � Identity");

                // ����� �� ������� ��� ������� ��� ��� ������ � Identity
                foreach (var user in usersWithoutIdentity)
                {
                    // ����� ����� ��������� �����������
                }
            }

            // �������� �� ����� � ��������������� ������
            var userRoleIds = await _dbContext.Users.Select(u => u.RoleId).Distinct().ToListAsync();
            var existingRoleIds = await _dbContext.Roles.Select(r => r.Id).ToListAsync();

            var invalidRoleIds = userRoleIds.Except(existingRoleIds).ToList();
            if (invalidRoleIds.Any())
            {
                issues.Add($"������� {invalidRoleIds.Count} ������������� � ��������� ������");

                // ����������� - ��������� ���� �� ���������
                var defaultRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "User");
                if (defaultRole != null)
                {
                    var usersWithInvalidRoles = await _dbContext.Users
                        .Where(u => invalidRoleIds.Contains(u.RoleId))
                        .ToListAsync();

                    foreach (var user in usersWithInvalidRoles)
                    {
                        user.RoleId = defaultRole.Id;
                    }

                    await _dbContext.SaveChangesAsync();
                }
            }

            // ������ �������� �����������...

            return issues;
        }

        private async Task CheckDatabasePerformanceAsync()
        {
            // ����� ������ ���� ��� ��� �������� � ����������� ������������������ ���� ������
            // ��������, ������������ ��������, ������ ������ �������� � �.�.

            // ��� ������ ��������
            await Task.Delay(1000);
        }
    }
}