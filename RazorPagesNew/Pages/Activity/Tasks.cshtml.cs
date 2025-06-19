using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RazorPagesNew.Models.Import;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Activity
{
    public class TasksModel : PageModel
    {
        private readonly ITaskRecordService _taskService;
        private readonly ITaskRecordExportService _exportService;
        private readonly IEmployeeService _employeeService;

        public TasksModel(
            ITaskRecordService taskService,
            ITaskRecordExportService exportService,
            IEmployeeService employeeService)
        {
            _taskService = taskService;
            _exportService = exportService;
            _employeeService = employeeService;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? EmployeeId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? DepartmentId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? MinImportance { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;

        public IEnumerable<TaskRecord> Tasks { get; set; }
        public SelectList EmployeeList { get; set; }
        public SelectList DepartmentList { get; set; }
        public SelectList ImportanceList { get; set; }

        public async Task<IActionResult> OnGetAsync(int? pageIndex)
        {
            // ��������� �������� �� ���������
            if (!StartDate.HasValue)
            {
                StartDate = DateTime.Now.AddMonths(-3);
            }

            if (!EndDate.HasValue)
            {
                EndDate = DateTime.Now;
            }

            // ����������� ������� ��������
            CurrentPage = pageIndex ?? 1;

            // ��������� ������ ������� � ����������� ��� �������
            var departments = await _employeeService.GetAllDepartmentsAsync();
            DepartmentList = new SelectList(departments, "Id", "Name");

            var employees = await _employeeService.GetAllEmployeesAsync();

            // ���������� ������ ����������� �� ���������� ������
            if (DepartmentId.HasValue)
            {
                employees = employees.Where(e => e.DepartmentId == DepartmentId.Value);
            }

            EmployeeList = new SelectList(employees, "Id", "FullName");

            // �������� ������ ��� ������� �� ��������
            var importanceItems = new[]
            {
                new { Id = 0, Name = "������" },
                new { Id = 1, Name = "�������" },
                new { Id = 2, Name = "�������" },
                new { Id = 3, Name = "�����������" }
            };
            ImportanceList = new SelectList(importanceItems, "Id", "Name");

            // ��������� ������ ����� � ������ ��������
            var allTasks = await _taskService.SearchTaskRecordsAsync(
                SearchTerm,
                EmployeeId,
                DepartmentId,
                StartDate,
                EndDate,
                MinImportance);

            // ���������� �����
            IOrderedEnumerable<TaskRecord> sortedTasks;

            switch (SortOrder)
            {
                case "date_asc":
                    sortedTasks = allTasks.OrderBy(t => t.CompletedAt);
                    break;
                case "title_desc":
                    sortedTasks = allTasks.OrderByDescending(t => t.Title);
                    break;
                case "title_asc":
                    sortedTasks = allTasks.OrderBy(t => t.Title);
                    break;
                case "importance_desc":
                    sortedTasks = allTasks.OrderByDescending(t => t.Importance);
                    break;
                case "importance_asc":
                    sortedTasks = allTasks.OrderBy(t => t.Importance);
                    break;
                case "efficiency_desc":
                    sortedTasks = allTasks.OrderByDescending(t => t.EfficiencyScore);
                    break;
                case "efficiency_asc":
                    sortedTasks = allTasks.OrderBy(t => t.EfficiencyScore);
                    break;
                default:
                    sortedTasks = allTasks.OrderByDescending(t => t.CompletedAt);
                    break;
            }

            // ������ ���������� �������
            var totalTasks = sortedTasks.Count();
            TotalPages = (int)Math.Ceiling(totalTasks / (double)PageSize);

            // ���������� ���������
            Tasks = sortedTasks
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return Page();
        }

        // ��������� ������ ��� ������������ ��������� � ����������� ���������� ����������
        public string GetPageUrl(int pageIndex)
        {
            return Url.Page("./Tasks", new
            {
                pageIndex,
                SearchTerm,
                EmployeeId,
                DepartmentId,
                StartDate,
                EndDate,
                MinImportance,
                SortOrder
            });
        }

        // ���������� ��������� ������������� �������� ������
        public string GetImportanceText(int importance)
        {
            return importance switch
            {
                0 => "������",
                1 => "�������",
                2 => "�������",
                3 => "�����������",
                _ => "�����������"
            };
        }

        // ���������� CSS ����� ��� ����������� �������� ������
        public string GetImportanceClass(int importance)
        {
            return importance switch
            {
                0 => "info",
                1 => "secondary",
                2 => "warning",
                3 => "danger",
                _ => "secondary"
            };
        }

        // ���������� CSS ����� ��� ����������� �������������
        public string GetEfficiencyClass(double? efficiency)
        {
            if (!efficiency.HasValue)
                return "secondary";

            return efficiency.Value switch
            {
                >= 80 => "success",
                >= 60 => "info",
                >= 40 => "warning",
                _ => "danger"
            };
        }

        // ���������� ��� �������� ������
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var result = await _taskService.DeleteTaskRecordAsync(id);

            if (result)
            {
                StatusMessage = "������ ������� �������.";
            }
            else
            {
                StatusMessage = "������ ��� �������� ������.";
            }

            return RedirectToPage();
        }

        // ���������� ��� �������� ����� � Excel
        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            // ��������� ������ ����� � ������ ��������
            var tasks = await _taskService.SearchTaskRecordsAsync(
                SearchTerm,
                EmployeeId,
                DepartmentId,
                StartDate,
                EndDate,
                MinImportance);

            // ����������� � DTO ��� ��������
            var exportTasks = await _exportService.ConvertToExportDtoAsync(tasks);

            // ������� � Excel
            var fileContent = await _exportService.ExportToExcelAsync(exportTasks);

            // ������������ ����� �����
            string fileName = $"������_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            // ���������� ���� ��� ����������
            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ���������� ��� �������� ����� � CSV
        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            // ��������� ������ ����� � ������ ��������
            var tasks = await _taskService.SearchTaskRecordsAsync(
                SearchTerm,
                EmployeeId,
                DepartmentId,
                StartDate,
                EndDate,
                MinImportance);

            // ����������� � DTO ��� ��������
            var exportTasks = await _exportService.ConvertToExportDtoAsync(tasks);

            // ������� � CSV
            var fileContent = await _exportService.ExportToCsvAsync(exportTasks, ";");

            // ������������ ����� �����
            string fileName = $"������_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            // ���������� ���� ��� ����������
            return File(fileContent, "text/csv", fileName);
        }

        // ���������� ��� �������� ����� � PDF
        public async Task<IActionResult> OnGetExportPdfAsync()
        {
            // ��������� ������ ����� � ������ ��������
            var tasks = await _taskService.SearchTaskRecordsAsync(
                SearchTerm,
                EmployeeId,
                DepartmentId,
                StartDate,
                EndDate,
                MinImportance);

            // ����������� � DTO ��� ��������
            var exportTasks = await _exportService.ConvertToExportDtoAsync(tasks);

            // ������������ ��������� ������
            string title = "����� �� ������� �����������";

            // ��������� ���������� � ��������
            if (EmployeeId.HasValue)
            {
                var employee = (await _employeeService.GetEmployeeByIdAsync(EmployeeId.Value))?.FullName;
                if (!string.IsNullOrEmpty(employee))
                {
                    title += $" - ���������: {employee}";
                }
            }
            else if (DepartmentId.HasValue)
            {
                var department = (await _employeeService.GetAllDepartmentsAsync())
                    .FirstOrDefault(d => d.Id == DepartmentId.Value)?.Name;
                if (!string.IsNullOrEmpty(department))
                {
                    title += $" - �����: {department}";
                }
            }

            // ������� � PDF
            var fileContent = await _exportService.ExportToPdfAsync(exportTasks, title);

            // ������������ ����� �����
            string fileName = $"������_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            // ���������� ���� ��� ����������
            return File(fileContent, "application/pdf", fileName);
        }
    }
}