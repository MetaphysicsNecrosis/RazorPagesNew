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
            // �����, ������ ���������� ��������
        }

        public async Task<IActionResult> OnPostSyncRolesAsync()
        {
            await _roleSyncService.SynchronizeRolesAsync();
            StatusMessage = "���� ������� ����������������";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSyncUsersAsync()
        {
            // ������������, ��� � ������� ������� ���� ����� ��� ������������� �������������
            // ���� ��� ���, ����� �������� � ��������� � ����������
            await _roleSyncService.SynchronizeUsersAsync();
            StatusMessage = "������������ ������� ����������������";
            return RedirectToPage();
        }
    }
}