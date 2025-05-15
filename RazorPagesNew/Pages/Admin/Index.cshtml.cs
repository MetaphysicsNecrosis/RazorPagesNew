using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesNew.Services.Interfaces;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Admin
{
    /*[Authorize(Roles = "Admin")]*/
    public class IndexModel : PageModel
    {
        private readonly IRoleSynchronizationService _roleSyncService;

        [TempData]
        public string StatusMessage { get; set; }

        public IndexModel(IRoleSynchronizationService roleSyncService)
        {
            _roleSyncService = roleSyncService;
        }

        public void OnGet()
        {
            // Пусто, просто отображаем страницу
        }

        public async Task<IActionResult> OnPostSyncRolesAsync()
        {
            await _roleSyncService.SynchronizeRolesAsync();
            StatusMessage = "Роли успешно синхронизированы";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSyncUsersAsync()
        {
            // Предполагаем, что в ролевом сервисе есть метод для синхронизации пользователей
            // Если его нет, нужно добавить в интерфейс и реализацию
            await _roleSyncService.SynchronizeUsersAsync();
            StatusMessage = "Пользователи успешно синхронизированы";
            return RedirectToPage();
        }
    }
}