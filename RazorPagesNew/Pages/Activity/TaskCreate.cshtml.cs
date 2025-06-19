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
                Importance = 1 // Средняя важность по умолчанию
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
                // Получаем ID текущего пользователя
                var currentUser = await _userService.GetCurrentUserAsync(User);
                int ownerId = currentUser?.Id ?? 1; // По умолчанию ID 1, если не удалось получить текущего пользователя

                // Создаем запись о задаче
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
                    StatusMessage = "Задача успешно создана.";
                    return RedirectToPage("./TaskDetails", new { id = result.Id });
                }
                else
                {
                    StatusMessage = "Ошибка при создании задачи.";
                    await LoadSelectListsAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при создании задачи: {ex.Message}";
                await LoadSelectListsAsync();
                return Page();
            }
        }

        private async Task LoadSelectListsAsync()
        {
            // Получаем список отделов и сотрудников для выпадающих списков
            var departments = await _employeeService.GetAllDepartmentsAsync();
            DepartmentList = new SelectList(departments, "Id", "Name");

            var employees = await _employeeService.GetAllEmployeesAsync();
            EmployeeList = new SelectList(employees, "Id", "FullName");

            // Список уровней важности
            var importanceItems = new[]
            {
                new { Id = 0, Name = "Низкая" },
                new { Id = 1, Name = "Средняя" },
                new { Id = 2, Name = "Высокая" },
                new { Id = 3, Name = "Критическая" }
            };
            ImportanceList = new SelectList(importanceItems, "Id", "Name");
        }

        // Возвращает сотрудников по отделу (для AJAX-запроса)
        public async Task<JsonResult> OnGetEmployeesByDepartmentAsync(int departmentId)
        {
            var employees = await _employeeService.GetEmployeesByDepartmentAsync(departmentId);
            return new JsonResult(employees.Select(e => new { e.Id, e.FullName }));
        }

        // Модель для ввода данных о задаче
        public class TaskInputModel
        {
            [Required(ErrorMessage = "Пожалуйста, выберите сотрудника")]
            [Display(Name = "Сотрудник")]
            public int? EmployeeId { get; set; }

            [Required(ErrorMessage = "Название задачи обязательно")]
            [StringLength(200, ErrorMessage = "Название задачи должно быть не более {1} символов")]
            [Display(Name = "Название задачи")]
            public string Title { get; set; }

            [Display(Name = "Описание задачи")]
            public string Description { get; set; }

            [Required(ErrorMessage = "Пожалуйста, укажите дату выполнения")]
            [Display(Name = "Дата выполнения")]
            [DataType(DataType.DateTime)]
            public DateTime CompletedAt { get; set; }

            [Display(Name = "Внешний ID")]
            [StringLength(100, ErrorMessage = "Внешний ID должен быть не более {1} символов")]
            public string ExternalSystemId { get; set; }

            [Display(Name = "Оценка эффективности (%)")]
            [Range(0, 100, ErrorMessage = "Оценка эффективности должна быть от {1} до {2}")]
            public double? EfficiencyScore { get; set; }

            [Required(ErrorMessage = "Пожалуйста, выберите важность задачи")]
            [Display(Name = "Важность")]
            [Range(0, 3, ErrorMessage = "Важность должна быть от {1} до {2}")]
            public int Importance { get; set; }
        }
    }
}