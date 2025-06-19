using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Activity
{
    public class TaskEditModel : PageModel
    {
        private readonly ITaskRecordService _taskService;
        private readonly IEmployeeService _employeeService;
        private readonly IUserService _userService;

        public TaskEditModel(
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
        public TaskEditViewModel Input { get; set; }

        public SelectList EmployeeList { get; set; }
        public SelectList ImportanceList { get; set; }
        public string CreatorName { get; set; }
        public DateTime CreatedAt { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var task = await _taskService.GetTaskRecordByIdAsync(id);

            if (task == null)
            {
                return NotFound();
            }

            // ��������� ������ ������� �� ������
            Input = new TaskEditViewModel
            {
                Id = task.Id,
                EmployeeId = task.EmployeeId,
                Title = task.Title,
                Description = task.Description,
                CompletedAt = task.CompletedAt,
                ExternalSystemId = task.ExternalSystemId,
                EfficiencyScore = task.EfficiencyScore,
                Importance = task.Importance
            };

            // �������� �������������� ����������
            CreatedAt = task.CreatedAt;

            if (task.Owner != null)
            {
                CreatorName = task.Owner.Username;
            }
            else
            {
                CreatorName = "�������";
            }

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
                // �������� ������������ ������
                var existingTask = await _taskService.GetTaskRecordByIdAsync(Input.Id);
                if (existingTask == null)
                {
                    return NotFound();
                }

                // ��������� ���� ������
                existingTask.EmployeeId = Input.EmployeeId;
                existingTask.Title = Input.Title;
                existingTask.Description = Input.Description;
                existingTask.CompletedAt = Input.CompletedAt;
                existingTask.ExternalSystemId = Input.ExternalSystemId;
                existingTask.EfficiencyScore = Input.EfficiencyScore;
                existingTask.Importance = Input.Importance;
                existingTask.UpdatedAt = DateTime.UtcNow;

                // ��������� ���������
                var result = await _taskService.UpdateTaskRecordAsync(existingTask);

                // ��������� ���������
                if (result != null)
                {
                    StatusMessage = "������ ������� ���������.";
                    return RedirectToPage("./TaskDetails", new { id = result.Id });
                }
                else
                {
                    StatusMessage = "������ ��� ���������� ������.";
                    await LoadSelectListsAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"������ ��� ���������� ������: {ex.Message}";
                await LoadSelectListsAsync();
                return Page();
            }
        }

        private async Task LoadSelectListsAsync()
        {
            // �������� ������ ����������� ��� ����������� ������
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

        // ������ ��� �������������� ������ � ������
        public class TaskEditViewModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "����������, �������� ����������")]
            [Display(Name = "���������")]
            public int EmployeeId { get; set; }

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