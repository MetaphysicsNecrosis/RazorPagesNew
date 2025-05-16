using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.ActivitySummaries
{
    public class GenerateModel : PageModel
    {
        private readonly IEvaluationService _evaluationService;
        private readonly IEmployeeService _employeeService;
        private readonly IAuditLogService _auditLogService;

        public GenerateModel(
            IEvaluationService evaluationService,
            IEmployeeService employeeService,
            IAuditLogService auditLogService)
        {
            _evaluationService = evaluationService;
            _employeeService = employeeService;
            _auditLogService = auditLogService;
        }

        [BindProperty]
        [Display(Name = "Дата начала")]
        [Required(ErrorMessage = "Дата начала периода обязательна")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [BindProperty]
        [Display(Name = "Дата окончания")]
        [Required(ErrorMessage = "Дата окончания периода обязательна")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [BindProperty]
        [Display(Name = "Перезаписать существующие сводки")]
        public bool OverwriteExisting { get; set; }

        [BindProperty]
        [Display(Name = "Отдел")]
        public int? DepartmentId { get; set; }

        [BindProperty]
        [Display(Name = "Выбранные сотрудники")]
        public List<int> SelectedEmployeeIds { get; set; }

        public List<Employee> Employees { get; set; }
        public SelectList DepartmentList { get; set; }
        public IEnumerable<WorkActivitySummary> GeneratedSummaries { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            // Set default period to current month
            var today = DateTime.Today;
            StartDate = new DateTime(today.Year, today.Month, 1);
            EndDate = today;

            await LoadFormData();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadFormData();
                return Page();
            }

            // Validate dates
            if (StartDate > EndDate)
            {
                ModelState.AddModelError("StartDate", "Дата начала не может быть позже даты окончания");
                await LoadFormData();
                return Page();
            }

            // Validate selected employees
            if (SelectedEmployeeIds == null || !SelectedEmployeeIds.Any())
            {
                ModelState.AddModelError("SelectedEmployeeIds", "Выберите хотя бы одного сотрудника");
                await LoadFormData();
                return Page();
            }

            try
            {
                // Generate summaries for selected employees
                var generatedSummaries = new List<WorkActivitySummary>();
                var currentUser = User.Identity.Name;

                foreach (var employeeId in SelectedEmployeeIds)
                {
                    // Check if summary already exists for this period
                    var existingSummaries = (await _evaluationService.GetWorkActivitySummariesByEmployeeIdAsync(employeeId))
                        .Where(s => (s.PeriodStart <= EndDate && s.PeriodEnd >= StartDate) ||
                               (s.PeriodStart >= StartDate && s.PeriodStart <= EndDate) ||
                               (s.PeriodEnd >= StartDate && s.PeriodEnd <= EndDate))
                        .ToList();

                    if (existingSummaries.Any() && !OverwriteExisting)
                    {
                        // Skip this employee because summaries exist and overwrite is not allowed
                        continue;
                    }

                    if (existingSummaries.Any() && OverwriteExisting)
                    {
                        // Delete existing summaries
                        foreach (var summary in existingSummaries)
                        {
                            await _evaluationService.DeleteWorkActivitySummaryAsync(summary.Id);

                            // Log the deletion
                            await _auditLogService.LogActivityAsync(
                                currentUser,
                                Models.Enums.ActionType.Delete,
                                "WorkActivitySummary",
                                summary.Id.ToString(),
                                $"Удалена сводка активности при перегенерации сводок"
                            );
                        }
                    }

                    // Generate new summary
                    var newSummary = await _evaluationService.GenerateWorkActivitySummaryAsync(employeeId, StartDate, EndDate);

                    if (newSummary != null)
                    {
                        // Set owner ID (current user)
                        var currentUserObj = await GetCurrentUserAsync();
                        if (currentUserObj != null)
                        {
                            newSummary.OwnerId = currentUserObj.Id;
                        }

                        // Save summary
                        await _evaluationService.CreateWorkActivitySummaryAsync(newSummary);

                        // Add to result list
                        generatedSummaries.Add(newSummary);

                        // Log the creation
                        await _auditLogService.LogActivityAsync(
                            currentUser,
                            Models.Enums.ActionType.Create,
                            "WorkActivitySummary",
                            newSummary.Id.ToString(),
                            $"Создана сводка активности за период {StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}"
                        );
                    }
                }

                // Store generated summaries for display
                GeneratedSummaries = generatedSummaries;

                // Set success message
                StatusMessage = $"Успешно сгенерировано {generatedSummaries.Count} сводок.";

                // Reload form data
                await LoadFormData();
                return Page();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
                await LoadFormData();
                return Page();
            }
        }

        private async Task LoadFormData()
        {
            // Load departments for dropdown
            var departments = await _employeeService.GetAllDepartmentsAsync();
            DepartmentList = new SelectList(departments, "Id", "Name");

            // Load all employees
            var allEmployees = await _employeeService.GetAllEmployeesAsync();
            Employees = allEmployees.ToList();

            // If no employees are selected yet, select all by default
            if (SelectedEmployeeIds == null || !SelectedEmployeeIds.Any())
            {
                SelectedEmployeeIds = Employees.Select(e => e.Id).ToList();
            }
        }

        private async Task<User> GetCurrentUserAsync()
        {
            // Get current user based on claims
            var currentUsername = User.Identity.Name;
            if (string.IsNullOrEmpty(currentUsername))
                return null;

            var allUsers = await ((IUserService)HttpContext.RequestServices.GetService(typeof(IUserService))).GetAllUsersAsync();
            return allUsers.FirstOrDefault(u => u.Username == currentUsername);
        }
    }
}