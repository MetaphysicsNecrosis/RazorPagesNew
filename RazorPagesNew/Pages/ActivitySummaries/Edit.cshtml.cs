
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.ActivitySummaries
{
    public class EditModel : PageModel
    {
        private readonly IEvaluationService _evaluationService;
        private readonly IEmployeeService _employeeService;
        private readonly IAuditLogService _auditLogService;

        public EditModel(
            IEvaluationService evaluationService,
            IEmployeeService employeeService,
            IAuditLogService auditLogService)
        {
            _evaluationService = evaluationService;
            _employeeService = employeeService;
            _auditLogService = auditLogService;
        }

        [BindProperty]
        public WorkActivitySummary Summary { get; set; }

        [BindProperty]
        public bool RecalculateScores { get; set; } = true;

        public Employee Employee { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Summary = await _evaluationService.GetWorkActivitySummaryByIdAsync(id);

            if (Summary == null)
            {
                return NotFound();
            }

            // Load employee details
            Employee = await _employeeService.GetEmployeeByIdAsync(Summary.EmployeeId);

            if (Employee == null)
            {
                return NotFound("Сотрудник не найден.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Reload employee details for the view
                Employee = await _employeeService.GetEmployeeByIdAsync(Summary.EmployeeId);
                return Page();
            }

            // Validate that start date is before end date
            if (Summary.PeriodStart > Summary.PeriodEnd)
            {
                ModelState.AddModelError("Summary.PeriodStart", "Дата начала не может быть позже даты окончания.");
                Employee = await _employeeService.GetEmployeeByIdAsync(Summary.EmployeeId);
                return Page();
            }

            try
            {
                // Get the original summary to preserve some fields
                var originalSummary = await _evaluationService.GetWorkActivitySummaryByIdAsync(Summary.Id);

                if (originalSummary == null)
                {
                    return NotFound();
                }

                // If recalculation is needed, recalculate scores based on the new data
                if (RecalculateScores)
                {
                    // Calculate working days in period
                    int workingDaysInPeriod = CountWorkingDays(Summary.PeriodStart, Summary.PeriodEnd);

                    // Recalculate attendance score (percent of working days attended)
                    Summary.AttendanceScore = workingDaysInPeriod > 0
                        ? Math.Min((double)Summary.AttendanceDays / workingDaysInPeriod * 100, 100)
                        : 0;

                    // Recalculate task score based on efficiency
                    Summary.TaskScore = Summary.AvgTaskEfficiency;

                    // Recalculate penalty score (inversely related to late arrivals)
                    Summary.PenaltyScore = Summary.AttendanceDays > 0
                        ? Math.Max(0, 100 - ((double)Summary.LateArrivals / Summary.AttendanceDays * 100))
                        : 0;
                }

                // Set update timestamp
                Summary.UpdatedAt = DateTime.UtcNow;

                // Update the summary
                await _evaluationService.UpdateWorkActivitySummaryAsync(Summary);

                // Log the update
                await _auditLogService.LogActivityAsync(
                    User.Identity.Name,
                    Models.Enums.ActionType.Update,
                    "WorkActivitySummary",
                    Summary.Id.ToString(),
                    $"Отредактирована сводка активности за период {Summary.PeriodStart:dd.MM.yyyy} - {Summary.PeriodEnd:dd.MM.yyyy} сотрудника {Employee.FullName}"
                );

                StatusMessage = "Сводка успешно обновлена.";

                return RedirectToPage("./Details", new { id = Summary.Id });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при обновлении сводки: {ex.Message}";
                Employee = await _employeeService.GetEmployeeByIdAsync(Summary.EmployeeId);
                return Page();
            }
        }

        private int CountWorkingDays(DateTime startDate, DateTime endDate)
        {
            int workingDays = 0;
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
            }
            return workingDays;
        }
    }
}