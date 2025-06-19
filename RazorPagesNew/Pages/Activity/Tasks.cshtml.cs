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
            // Установка значений по умолчанию
            if (!StartDate.HasValue)
            {
                StartDate = DateTime.Now.AddMonths(-3);
            }

            if (!EndDate.HasValue)
            {
                EndDate = DateTime.Now;
            }

            // Определение текущей страницы
            CurrentPage = pageIndex ?? 1;

            // Получение списка отделов и сотрудников для фильтра
            var departments = await _employeeService.GetAllDepartmentsAsync();
            DepartmentList = new SelectList(departments, "Id", "Name");

            var employees = await _employeeService.GetAllEmployeesAsync();

            // Фильтрация списка сотрудников по выбранному отделу
            if (DepartmentId.HasValue)
            {
                employees = employees.Where(e => e.DepartmentId == DepartmentId.Value);
            }

            EmployeeList = new SelectList(employees, "Id", "FullName");

            // Создание списка для фильтра по важности
            var importanceItems = new[]
            {
                new { Id = 0, Name = "Низкая" },
                new { Id = 1, Name = "Средняя" },
                new { Id = 2, Name = "Высокая" },
                new { Id = 3, Name = "Критическая" }
            };
            ImportanceList = new SelectList(importanceItems, "Id", "Name");

            // Получение списка задач с учетом фильтров
            var allTasks = await _taskService.SearchTaskRecordsAsync(
                SearchTerm,
                EmployeeId,
                DepartmentId,
                StartDate,
                EndDate,
                MinImportance);

            // Сортировка задач
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

            // Расчет количества страниц
            var totalTasks = sortedTasks.Count();
            TotalPages = (int)Math.Ceiling(totalTasks / (double)PageSize);

            // Применение пагинации
            Tasks = sortedTasks
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return Page();
        }

        // Генерация ссылки для постраничной навигации с сохранением параметров фильтрации
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

        // Обработчик для удаления задачи
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var result = await _taskService.DeleteTaskRecordAsync(id);

            if (result)
            {
                StatusMessage = "Задача успешно удалена.";
            }
            else
            {
                StatusMessage = "Ошибка при удалении задачи.";
            }

            return RedirectToPage();
        }

        // Обработчик для экспорта задач в Excel
        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            // Получение списка задач с учетом фильтров
            var tasks = await _taskService.SearchTaskRecordsAsync(
                SearchTerm,
                EmployeeId,
                DepartmentId,
                StartDate,
                EndDate,
                MinImportance);

            // Конвертация в DTO для экспорта
            var exportTasks = await _exportService.ConvertToExportDtoAsync(tasks);

            // Экспорт в Excel
            var fileContent = await _exportService.ExportToExcelAsync(exportTasks);

            // Формирование имени файла
            string fileName = $"Задачи_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            // Возвращаем файл для скачивания
            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // Обработчик для экспорта задач в CSV
        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            // Получение списка задач с учетом фильтров
            var tasks = await _taskService.SearchTaskRecordsAsync(
                SearchTerm,
                EmployeeId,
                DepartmentId,
                StartDate,
                EndDate,
                MinImportance);

            // Конвертация в DTO для экспорта
            var exportTasks = await _exportService.ConvertToExportDtoAsync(tasks);

            // Экспорт в CSV
            var fileContent = await _exportService.ExportToCsvAsync(exportTasks, ";");

            // Формирование имени файла
            string fileName = $"Задачи_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            // Возвращаем файл для скачивания
            return File(fileContent, "text/csv", fileName);
        }

        // Обработчик для экспорта задач в PDF
        public async Task<IActionResult> OnGetExportPdfAsync()
        {
            // Получение списка задач с учетом фильтров
            var tasks = await _taskService.SearchTaskRecordsAsync(
                SearchTerm,
                EmployeeId,
                DepartmentId,
                StartDate,
                EndDate,
                MinImportance);

            // Конвертация в DTO для экспорта
            var exportTasks = await _exportService.ConvertToExportDtoAsync(tasks);

            // Формирование заголовка отчета
            string title = "Отчет по задачам сотрудников";

            // Добавляем информацию о фильтрах
            if (EmployeeId.HasValue)
            {
                var employee = (await _employeeService.GetEmployeeByIdAsync(EmployeeId.Value))?.FullName;
                if (!string.IsNullOrEmpty(employee))
                {
                    title += $" - Сотрудник: {employee}";
                }
            }
            else if (DepartmentId.HasValue)
            {
                var department = (await _employeeService.GetAllDepartmentsAsync())
                    .FirstOrDefault(d => d.Id == DepartmentId.Value)?.Name;
                if (!string.IsNullOrEmpty(department))
                {
                    title += $" - Отдел: {department}";
                }
            }

            // Экспорт в PDF
            var fileContent = await _exportService.ExportToPdfAsync(exportTasks, title);

            // Формирование имени файла
            string fileName = $"Задачи_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            // Возвращаем файл для скачивания
            return File(fileContent, "application/pdf", fileName);
        }
    }
}