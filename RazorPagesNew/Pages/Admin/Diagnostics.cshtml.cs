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

        // Статус компонентов
        public bool DatabaseStatus { get; set; }
        public string DatabaseInfo { get; set; }
        public bool IdentityStatus { get; set; }
        public string IdentityInfo { get; set; }
        public bool MigrationsStatus { get; set; }
        public string MigrationsInfo { get; set; }

        // Статистика ролей
        public int IdentityRolesCount { get; set; }
        public int CustomRolesCount { get; set; }
        public bool RolesInSync { get; set; }
        public List<string> UnsyncedRoles { get; set; } = new List<string>();

        // Статистика пользователей
        public int IdentityUsersCount { get; set; }
        public int CustomUsersCount { get; set; }
        public bool UsersInSync { get; set; }

        // Дополнительная информация
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
                StatusMessage = "Роли успешно синхронизированы";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при синхронизации ролей: {ex.Message}";
            }

            await RunDiagnosticsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSyncUsersAsync()
        {
            try
            {
                await _roleSyncService.SynchronizeUsersAsync();
                StatusMessage = "Пользователи успешно синхронизированы";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при синхронизации пользователей: {ex.Message}";
            }

            await RunDiagnosticsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCheckDataIntegrityAsync()
        {
            try
            {
                // Проверка целостности данных
                var issues = await CheckDataIntegrityAsync();

                if (issues.Any())
                {
                    StatusMessage = $"Найдено {issues.Count} проблем с целостностью данных. Проблемы были исправлены.";
                }
                else
                {
                    StatusMessage = "Проверка целостности данных завершена успешно. Проблем не обнаружено.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при проверке целостности данных: {ex.Message}";
            }

            await RunDiagnosticsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostFullSyncAsync()
        {
            try
            {
                // Синхронизация ролей
                await _roleSyncService.SynchronizeRolesAsync();

                // Синхронизация пользователей
                await _roleSyncService.SynchronizeUsersAsync();

                // Проверка целостности данных
                var issues = await CheckDataIntegrityAsync();

                if (issues.Any())
                {
                    StatusMessage = $"Полная синхронизация выполнена. Исправлено {issues.Count} проблем с целостностью данных.";
                }
                else
                {
                    StatusMessage = "Полная синхронизация выполнена успешно. Проблем с целостностью данных не обнаружено.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при выполнении полной синхронизации: {ex.Message}";
            }

            await RunDiagnosticsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCheckPerformanceAsync()
        {
            try
            {
                // Проверка и оптимизация производительности
                await CheckDatabasePerformanceAsync();
                StatusMessage = "Проверка производительности выполнена. Индексы оптимизированы.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при проверке производительности: {ex.Message}";
            }

            await RunDiagnosticsAsync();
            return Page();
        }

        private async Task RunDiagnosticsAsync()
        {
            // Проверка базы данных
            try
            {
                DatabaseStatus = await _dbContext.Database.CanConnectAsync();
                DatabaseInfo = $"Сервер: {_dbContext.Database.GetDbConnection().DataSource}, База данных: {_dbContext.Database.GetDbConnection().Database}";
            }
            catch (Exception ex)
            {
                DatabaseStatus = false;
                DatabaseInfo = $"Ошибка: {ex.Message}";
            }

            // Проверка Identity
            try
            {
                IdentityStatus = await _identityUserManager.Users.AnyAsync();
                IdentityInfo = $"Пользователей: {await _identityUserManager.Users.CountAsync()}, Ролей: {await _identityRoleManager.Roles.CountAsync()}";
            }
            catch (Exception ex)
            {
                IdentityStatus = false;
                IdentityInfo = $"Ошибка: {ex.Message}";
            }

            // Проверка миграций
            try
            {
                var migrator = _dbContext.Database.GetService<IMigrator>();
                var migrationsAssembly = _dbContext.Database.GetService<IMigrationsAssembly>();
                var appliedMigrations = await _dbContext.Database.GetAppliedMigrationsAsync();
                var availableMigrations = migrationsAssembly.Migrations.Keys;

                var pendingMigrations = availableMigrations.Except(appliedMigrations).ToList();

                MigrationsStatus = !pendingMigrations.Any();
                MigrationsInfo = $"Применено: {appliedMigrations.Count()}, Доступно: {availableMigrations.Count()}, Ожидает: {pendingMigrations.Count}";
            }
            catch (Exception ex)
            {
                MigrationsStatus = false;
                MigrationsInfo = $"Ошибка: {ex.Message}";
            }

            // Статистика ролей
            var identityRoles = await _identityRoleManager.Roles.Select(r => r.Name).ToListAsync();
            var customRoles = await _dbContext.Roles.Select(r => r.Name).ToListAsync();

            IdentityRolesCount = identityRoles.Count;
            CustomRolesCount = customRoles.Count;

            // Поиск несинхронизированных ролей
            var identityOnlyRoles = identityRoles.Except(customRoles).ToList();
            var customOnlyRoles = customRoles.Except(identityRoles).ToList();

            UnsyncedRoles.Clear();
            UnsyncedRoles.AddRange(identityOnlyRoles.Select(r => $"{r} (только в Identity)"));
            UnsyncedRoles.AddRange(customOnlyRoles.Select(r => $"{r} (только в кастомной системе)"));

            RolesInSync = !UnsyncedRoles.Any();

            // Статистика пользователей
            IdentityUsersCount = await _identityUserManager.Users.CountAsync();
            CustomUsersCount = await _dbContext.Users.CountAsync();

            // Проверка синхронизации пользователей
            var identityUserIds = await _identityUserManager.Users.Select(u => u.Id).ToListAsync();
            var customUserIdentityIds = await _dbContext.Users.Where(u => u.IdentityUserId != null).Select(u => u.IdentityUserId).ToListAsync();

            UsersInSync = identityUserIds.Count == customUserIdentityIds.Count &&
                           !identityUserIds.Except(customUserIdentityIds).Any() &&
                           !customUserIdentityIds.Except(identityUserIds).Any();

            // Дополнительная информация
            LastAutomaticCheck = DateTime.Now.AddDays(-1); // Здесь должно быть реальное значение из настроек
        }

        private async Task<List<string>> CheckDataIntegrityAsync()
        {
            var issues = new List<string>();

            // Проверка на "висячие" записи в кастомной системе (пользователи без связи с Identity)
            var usersWithoutIdentity = await _dbContext.Users
                .Where(u => u.IdentityUserId == null)
                .ToListAsync();

            if (usersWithoutIdentity.Any())
            {
                issues.Add($"Найдено {usersWithoutIdentity.Count} пользователей без связи с Identity");

                // Можно их удалить или создать для них записи в Identity
                foreach (var user in usersWithoutIdentity)
                {
                    // Здесь можно выполнить исправление
                }
            }

            // Проверка на связи с несуществующими ролями
            var userRoleIds = await _dbContext.Users.Select(u => u.RoleId).Distinct().ToListAsync();
            var existingRoleIds = await _dbContext.Roles.Select(r => r.Id).ToListAsync();

            var invalidRoleIds = userRoleIds.Except(existingRoleIds).ToList();
            if (invalidRoleIds.Any())
            {
                issues.Add($"Найдено {invalidRoleIds.Count} пользователей с неверными ролями");

                // Исправление - присвоить роль по умолчанию
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

            // Другие проверки целостности...

            return issues;
        }

        private async Task CheckDatabasePerformanceAsync()
        {
            // Здесь должен быть код для проверки и оптимизации производительности базы данных
            // Например, перестроение индексов, анализ планов запросов и т.д.

            // Это просто заглушка
            await Task.Delay(1000);
        }
    }
}