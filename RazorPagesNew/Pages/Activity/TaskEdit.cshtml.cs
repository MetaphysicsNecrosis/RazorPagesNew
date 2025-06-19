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

            // Заполняем модель данными из задачи
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

            // Получаем дополнительную информацию
            CreatedAt = task.CreatedAt;

            if (task.Owner != null)
            {
                CreatorName = task.Owner.Username;
            }
            else
            {
                CreatorName = "Система";
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
                // Получаем существующую задачу
                var existingTask = await _taskService.GetTaskRecordByIdAsync(Input.Id);
                if (existingTask == null)
                {
                    return NotFound();
                }

                // Обновляем поля задачи
                existingTask.EmployeeId = Input.EmployeeId;
                existingTask.Title = Input.Title;
                existingTask.Description = Input.Description;
                existingTask.CompletedAt = Input.CompletedAt;
                existingTask.ExternalSystemId = Input.ExternalSystemId;
                existingTask.EfficiencyScore = Input.EfficiencyScore;
                existingTask.Importance = Input.Importance;
                existingTask.UpdatedAt = DateTime.UtcNow;

                // Сохраняем изменения
                var result = await _taskService.UpdateTaskRecordAsync(existingTask);

                // Проверяем результат
                if (result != null)
                {
                    StatusMessage = "Задача успешно обновлена.";
                    return RedirectToPage("./TaskDetails", new { id = result.Id });
                }
                else
                {
                    StatusMessage = "Ошибка при обновлении задачи.";
                    await LoadSelectListsAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при обновлении задачи: {ex.Message}";
                await LoadSelectListsAsync();
                return Page();
            }
        }

        private async Task LoadSelectListsAsync()
        {
            // Получаем список сотрудников для выпадающего списка
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

        // Модель для редактирования данных о задаче
        public class TaskEditViewModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "Пожалуйста, выберите сотрудника")]
            [Display(Name = "Сотрудник")]
            public int EmployeeId { get; set; }

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