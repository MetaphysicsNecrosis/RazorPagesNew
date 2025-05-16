using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.ActivitySummaries
{
    public class IndexModel : PageModel
    {
        private readonly IEvaluationService _evaluationService;
        private readonly IEmployeeService _employeeService;
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditLogService;

        public IndexModel(
            IEvaluationService evaluationService,
            IEmployeeService employeeService,
            IUserService userService,
            IAuditLogService auditLogService)
        {
            _evaluationService = evaluationService;
            _employeeService = employeeService;
            _userService = userService;
            _auditLogService = auditLogService;
        }

        public IList<WorkActivitySummary> Summaries { get; set; } = new List<WorkActivitySummary>();
        public SelectList DepartmentList { get; set; }
        public SelectList EmployeeList { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? DepartmentId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? EmployeeId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalSummaries { get; set; }
        public double AverageAttendanceScore { get; set; }
        public double AverageTaskScore { get; set; }

        // Chart data properties
        public object AttendanceChartData { get; set; }
        public object TaskChartData { get; set; }
        public object DepartmentChartData { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            // Set default dates if not provided
            StartDate ??= DateTime.Today.AddMonths(-3);
            EndDate ??= DateTime.Today;

            // Load departments for filtering
            var departments = await _employeeService.GetAllDepartmentsAsync();
            DepartmentList = new SelectList(departments, "Id", "Name");

            // Load employees for filtering
            var employees = await _employeeService.GetAllEmployeesAsync();

            // Filter employees by department if selected
            if (DepartmentId.HasValue)
            {
                employees = employees.Where(e => e.DepartmentId == DepartmentId.Value).ToList();
            }

            EmployeeList = new SelectList(employees, "Id", "FullName");

            // Fetch summaries with filtering
            var summariesQuery = await FetchSummariesAsync();

            // Get the total count for pagination
            TotalSummaries = summariesQuery.Count;

            // Calculate the total pages
            TotalPages = (int)Math.Ceiling(TotalSummaries / (double)PageSize);

            // Ensure current page is valid
            if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }
            else if (CurrentPage > TotalPages && TotalPages > 0)
            {
                CurrentPage = TotalPages;
            }

            // Get the paged summaries
            Summaries = summariesQuery
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Calculate average scores
            if (summariesQuery.Any())
            {
                AverageAttendanceScore = summariesQuery.Average(s => s.AttendanceScore);
                AverageTaskScore = summariesQuery.Average(s => s.TaskScore);
            }

            // Prepare chart data
            PrepareChartData(summariesQuery);
        }

        // Handler for deleting a summary
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var summary = await _evaluationService.GetWorkActivitySummaryByIdAsync(id);
            if (summary == null)
            {
                return NotFound();
            }

            var result = await _evaluationService.DeleteWorkActivitySummaryAsync(id);
            if (result)
            {
                // Log the deletion
                await _auditLogService.LogActivityAsync(
                    User.Identity.Name,
                    Models.Enums.ActionType.Delete,
                    "WorkActivitySummary",
                    id.ToString(),
                    $"Удалена сводка активности за период {summary.PeriodStart:dd.MM.yyyy} - {summary.PeriodEnd:dd.MM.yyyy} сотрудника {summary.Employee.FullName}"
                );

                StatusMessage = "Сводка успешно удалена.";
            }
            else
            {
                StatusMessage = "Ошибка при удалении сводки.";
            }

            return RedirectToPage();
        }

        // Helper method to construct page URL for pagination
        public string GetPageUrl(int pageIndex)
        {
            var routeValues = new Dictionary<string, string>
            {
                { "pageIndex", pageIndex.ToString() }
            };

            if (DepartmentId.HasValue)
            {
                routeValues.Add("departmentId", DepartmentId.Value.ToString());
            }

            if (EmployeeId.HasValue)
            {
                routeValues.Add("employeeId", EmployeeId.Value.ToString());
            }

            if (StartDate.HasValue)
            {
                routeValues.Add("startDate", StartDate.Value.ToString("yyyy-MM-dd"));
            }

            if (EndDate.HasValue)
            {
                routeValues.Add("endDate", EndDate.Value.ToString("yyyy-MM-dd"));
            }

            return Url.Page("./Index", routeValues);
        }

        // Helper method to fetch summaries with filtering
        private async Task<List<WorkActivitySummary>> FetchSummariesAsync()
        {
            List<WorkActivitySummary> allSummaries = new List<WorkActivitySummary>();

            // Get the summaries based on the filter criteria
            if (EmployeeId.HasValue)
            {
                // Get summaries for a specific employee
                var employeeSummaries = await _evaluationService.GetWorkActivitySummariesByEmployeeIdAsync(EmployeeId.Value);
                allSummaries.AddRange(employeeSummaries);
            }
            else
            {
                // Get all employees
                var employees = await _employeeService.GetAllEmployeesAsync();

                // Filter by department if specified
                if (DepartmentId.HasValue)
                {
                    employees = employees.Where(e => e.DepartmentId == DepartmentId.Value).ToList();
                }

                // Get summaries for each employee
                foreach (var employee in employees)
                {
                    var employeeSummaries = await _evaluationService.GetWorkActivitySummariesByEmployeeIdAsync(employee.Id);
                    allSummaries.AddRange(employeeSummaries);
                }
            }

            // Apply date filters if specified
            var filteredSummaries = allSummaries;

            if (StartDate.HasValue)
            {
                filteredSummaries = filteredSummaries.Where(s => s.PeriodEnd >= StartDate).ToList();
            }

            if (EndDate.HasValue)
            {
                filteredSummaries = filteredSummaries.Where(s => s.PeriodStart <= EndDate).ToList();
            }

            // Sort by period end date (most recent first)
            return filteredSummaries.OrderByDescending(s => s.PeriodEnd).ToList();
        }

        // Prepare chart data for the visualizations
        private void PrepareChartData(List<WorkActivitySummary> summaries)
        {
            if (!summaries.Any())
            {
                // No data to visualize
                return;
            }

            // Group summaries by period (month/year) for the time-based charts
            var groupedByPeriod = summaries
                .GroupBy(s => new { Year = s.PeriodEnd.Year, Month = s.PeriodEnd.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Summaries = g.ToList()
                })
                .ToList();

            // Prepare attendance chart data
            var attendanceData = new
            {
                labels = groupedByPeriod.Select(g => g.Period).ToArray(),
                attendanceDays = groupedByPeriod.Select(g => g.Summaries.Average(s => s.AttendanceDays)).ToArray(),
                lateArrivals = groupedByPeriod.Select(g => g.Summaries.Average(s => s.LateArrivals)).ToArray()
            };
            AttendanceChartData = attendanceData;

            // Prepare task chart data
            var taskData = new
            {
                labels = groupedByPeriod.Select(g => g.Period).ToArray(),
                completedTasks = groupedByPeriod.Select(g => g.Summaries.Average(s => s.CompletedTasks)).ToArray(),
                efficiency = groupedByPeriod.Select(g => g.Summaries.Average(s => s.AvgTaskEfficiency)).ToArray()
            };
            TaskChartData = taskData;

            // Prepare department comparison chart data
            if (DepartmentId == null)
            {
                // No department selected, show all departments
                var departmentGroups = summaries
                    .GroupBy(s => s.Employee.Department.Name)
                    .Select(g => new
                    {
                        DepartmentName = g.Key,
                        AttendanceScore = g.Average(s => s.AttendanceScore),
                        TaskScore = g.Average(s => s.TaskScore),
                        PenaltyScore = g.Average(s => s.PenaltyScore)
                    })
                    .ToList();

                var departmentData = new
                {
                    departments = departmentGroups.Select(d => d.DepartmentName).ToArray(),
                    attendanceScores = departmentGroups.Select(d => d.AttendanceScore).ToArray(),
                    taskScores = departmentGroups.Select(d => d.TaskScore).ToArray(),
                    penaltyScores = departmentGroups.Select(d => d.PenaltyScore).ToArray()
                };
                DepartmentChartData = departmentData;
            }
            else
            {
                // Department selected, show employees in that department
                var employeeGroups = summaries
                    .Where(s => s.Employee.DepartmentId == DepartmentId)
                    .GroupBy(s => s.Employee.FullName)
                    .Select(g => new
                    {
                        EmployeeName = g.Key,
                        AttendanceScore = g.Average(s => s.AttendanceScore),
                        TaskScore = g.Average(s => s.TaskScore),
                        PenaltyScore = g.Average(s => s.PenaltyScore)
                    })
                    .ToList();

                var departmentData = new
                {
                    departments = employeeGroups.Select(e => e.EmployeeName).ToArray(),
                    attendanceScores = employeeGroups.Select(e => e.AttendanceScore).ToArray(),
                    taskScores = employeeGroups.Select(e => e.TaskScore).ToArray(),
                    penaltyScores = employeeGroups.Select(e => e.PenaltyScore).ToArray()
                };
                DepartmentChartData = departmentData;
            }
        }
    }
}