using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Activity
{
    public class TaskDetailsModel : PageModel
    {
        private readonly ITaskRecordService _taskService;
        private readonly IEmployeeService _employeeService;

        public TaskDetailsModel(
            ITaskRecordService taskService,
            IEmployeeService employeeService)
        {
            _taskService = taskService;
            _employeeService = employeeService;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public TaskRecord Task { get; set; }
        public string OwnerUsername { get; set; }
        public string DepartmentName { get; set; }
        public double? AverageEfficiency { get; set; }

        public async System.Threading.Tasks.Task<IActionResult> OnGetAsync(int id)
        {
            Task = await _taskService.GetTaskRecordByIdAsync(id);

            if (Task == null)
            {
                return NotFound();
            }

            // Получаем имя отдела
            var employee = await _employeeService.GetEmployeeByIdAsync(Task.EmployeeId);
            if (employee != null && employee.Department != null)
            {
                DepartmentName = employee.Department.Name;
            }
            else
            {
                DepartmentName = "Не указан";
            }

            // Получаем имя владельца записи
            if (Task.Owner != null)
            {
                OwnerUsername = Task.Owner.Username;
            }
            else
            {
                OwnerUsername = "Система";
            }

            // Получаем среднюю эффективность сотрудника за период (±1 месяц от даты выполнения задачи)
            var startDate = Task.CompletedAt.AddMonths(-1);
            var endDate = Task.CompletedAt.AddMonths(1);
            AverageEfficiency = await _taskService.GetAverageEfficiencyAsync(Task.EmployeeId, startDate, endDate);

            return Page();
        }

        // Возвращает текстовое представление важности задачи
        public string GetImportanceText(int importance)
        {
            return importance switch
            {
                0 => "Низкая",
                1 => "Средняя",
                2 => "Высокая",
                3 => "Критическая",
                _ => "Неизвестная"
            };
        }

        // Возвращает CSS класс для отображения важности задачи
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

        // Возвращает CSS класс для отображения эффективности
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
    }
}