using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Activity
{
    public class TaskCreateModel : PageModel
    {
        private readonly ITaskRecordService _taskService;
        private readonly IEmployeeService _employeeService;
        private readonly IUserService _userService;

        public TaskCreateModel(
            ITaskRecordService taskService,
            IEmployeeService employeeService,
            IUserService userService)
        {
            _taskService = taskService;
            _employeeService = employeeService;
            _userService = userService;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public TaskInputModel Input { get; set; }

        public SelectList EmployeeList { get; set; }
        public SelectList DepartmentList { get; set; }
        public SelectList ImportanceList { get; set; }

        public async Task<IActionResult> OnGetAsync(int? employeeId = null)
        {
            Input = new TaskInputModel
            {
                CompletedAt = DateTime.Now,
                EmployeeId = employeeId,
                Importance = 1 // ������� �������� �� ���������
            };

            await LoadSelectListsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync();
                return Page();
            }

            try
            {
                // �������� ID �������� ������������
                var currentUser = await _userService.GetCurrentUserAsync(User);
                int ownerId = currentUser?.Id ?? 1; // �� ��������� ID 1, ���� �� ������� �������� �������� ������������

                // ������� ������ � ������
                var task = new TaskRecord
                {
                    EmployeeId = Input.EmployeeId.Value,
                    Title = Input.Title,
                    Description = Input.Description,
                    CompletedAt = Input.CompletedAt,
                    ExternalSystemId = Input.ExternalSystemId,
                    EfficiencyScore = Input.EfficiencyScore,
                    Importance = Input.Importance,
                    OwnerId = ownerId,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _taskService.CreateTaskRecordAsync(task);

                if (result != null)
                {
                    StatusMessage = "������ ������� �������.";
                    return RedirectToPage("./TaskDetails", new { id = result.Id });
                }
                else
                {
                    StatusMessage = "������ ��� �������� ������.";
                    await LoadSelectListsAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"������ ��� �������� ������: {ex.Message}";
                await LoadSelectListsAsync();
                return Page();
            }
        }

        private async Task LoadSelectListsAsync()
        {
            // �������� ������ ������� � ����������� ��� ���������� �������
            var departments = await _employeeService.GetAllDepartmentsAsync();
            DepartmentList = new SelectList(departments, "Id", "Name");

            var employees = await _employeeService.GetAllEmployeesAsync();
            EmployeeList = new SelectList(employees, "Id", "FullName");

            // ������ ������� ��������
            var importanceItems = new[]
            {
                new { Id = 0, Name = "������" },
                new { Id = 1, Name = "�������" },
                new { Id = 2, Name = "�������" },
                new { Id = 3, Name = "�����������" }
            };
            ImportanceList = new SelectList(importanceItems, "Id", "Name");
        }

        // ���������� ����������� �� ������ (��� AJAX-�������)
        public async Task<JsonResult> OnGetEmployeesByDepartmentAsync(int departmentId)
        {
            var employees = await _employeeService.GetEmployeesByDepartmentAsync(departmentId);
            return new JsonResult(employees.Select(e => new { e.Id, e.FullName }));
        }

        // ������ ��� ����� ������ � ������
        public class TaskInputModel
        {
            [Required(ErrorMessage = "����������, �������� ����������")]
            [Display(Name = "���������")]
            public int? EmployeeId { get; set; }

            [Required(ErrorMessage = "�������� ������ �����������")]
            [StringLength(200, ErrorMessage = "�������� ������ ������ ���� �� ����� {1} ��������")]
            [Display(Name = "�������� ������")]
            public string Title { get; set; }

            [Display(Name = "�������� ������")]
            public string Description { get; set; }

            [Required(ErrorMessage = "����������, ������� ���� ����������")]
            [Display(Name = "���� ����������")]
            [DataType(DataType.DateTime)]
            public DateTime CompletedAt { get; set; }

            [Display(Name = "������� ID")]
            [StringLength(100, ErrorMessage = "������� ID ������ ���� �� ����� {1} ��������")]
            public string ExternalSystemId { get; set; }

            [Display(Name = "������ ������������� (%)")]
            [Range(0, 100, ErrorMessage = "������ ������������� ������ ���� �� {1} �� {2}")]
            public double? EfficiencyScore { get; set; }

            [Required(ErrorMessage = "����������, �������� �������� ������")]
            [Display(Name = "��������")]
            [Range(0, 3, ErrorMessage = "�������� ������ ���� �� {1} �� {2}")]
            public int Importance { get; set; }
        }
    }
}