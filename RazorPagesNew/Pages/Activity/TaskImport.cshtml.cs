using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RazorPagesNew.Models.Import;
using RazorPagesNew.Services.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Activity
{
    public class TaskImportModel : PageModel
    {
        private readonly ITaskRecordImportService _importService;
        private readonly IEmployeeService _employeeService;
        private readonly IUserService _userService;

        public TaskImportModel(
            ITaskRecordImportService importService,
            IEmployeeService employeeService,
            IUserService userService)
        {
            _importService = importService;
            _employeeService = employeeService;
            _userService = userService;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public ImportForm Input { get; set; }

        public SelectList DepartmentList { get; set; }
        public TaskImportResult ImportResult { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Input = new ImportForm
            {
                SkipHeader = true
            };

            await LoadDepartmentsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDepartmentsAsync();
                return Page();
            }

            try
            {
                if (Input.ImportFile == null || Input.ImportFile.Length == 0)
                {
                    ModelState.AddModelError("Input.ImportFile", "����������, �������� ���� ��� �������.");
                    await LoadDepartmentsAsync();
                    return Page();
                }

                // �������� �������� ������������
                var currentUser = await _userService.GetCurrentUserAsync(User);
                string username = currentUser?.Username ?? User.Identity.Name ?? "system";

                // ������ ���������� �����
                byte[] fileContent;
                using (var memoryStream = new MemoryStream())
                {
                    await Input.ImportFile.CopyToAsync(memoryStream);
                    fileContent = memoryStream.ToArray();
                }

                // ������ ���������� �����
                var tasks = await _importService.ParseImportFileAsync(
                    fileContent,
                    Input.ImportFile.FileName,
                    Input.SkipHeader);

                // ����������� ������
                ImportResult = await _importService.ImportTasksAsync(
                    tasks,
                    Input.DefaultDepartmentId,
                    Input.UpdateExisting,
                    username);

                // ��������� ���������� � �����
                ImportResult.FileName = Input.ImportFile.FileName;
                ImportResult.FileType = Path.GetExtension(Input.ImportFile.FileName).ToLower();

                // ��������� ��������� � ����������
                StatusMessage = ImportResult.Message;

                return Page();
            }
            catch (Exception ex)
            {
                StatusMessage = $"������ ��� �������: {ex.Message}";
                await LoadDepartmentsAsync();
                return Page();
            }
        }

        private async Task LoadDepartmentsAsync()
        {
            var departments = await _employeeService.GetAllDepartmentsAsync();
            DepartmentList = new SelectList(departments, "Id", "Name");
        }

        public class ImportForm
        {
            [Required(ErrorMessage = "�������� ���� ��� �������")]
            [Display(Name = "���� ��� �������")]
            public IFormFile ImportFile { get; set; }

            [Required(ErrorMessage = "�������� ����� �� ���������")]
            [Display(Name = "����� �� ���������")]
            public int DefaultDepartmentId { get; set; }

            [Display(Name = "��������� ������������ ������")]
            public bool UpdateExisting { get; set; }

            [Display(Name = "���������� ��������� �������")]
            public bool SkipHeader { get; set; }
        }
    }
}