using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public Dictionary<int, Employee> EmployeeDict { get; set; } = new Dictionary<int, Employee>();
        public Dictionary<int, Department> DepartmentDict { get; set; } = new Dictionary<int, Department>();
        public Dictionary<int, User> UserDict { get; set; } = new Dictionary<int, User>();
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

            // Load all necessary data upfront
            await LoadDictionariesAsync();

            // Set up department list for filtering
            var departmentItems = new List<SelectListItem>();
            foreach (var department in DepartmentDict.Values)
            {
                departmentItems.Add(new SelectListItem
                {
                    Value = department.Id.ToString(),
                    Text = department.Name
                });
            }
            DepartmentList = new SelectList(departmentItems, "Value", "Text");

            // Set up employee list for filtering
            var employeeItems = new List<SelectListItem>();
            foreach (var employee in EmployeeDict.Values)
            {
                // If department filter is applied, only include employees from that department
                if (!DepartmentId.HasValue || employee.DepartmentId == DepartmentId.Value)
                {
                    string departmentName = "";
                    if (DepartmentDict.ContainsKey(employee.DepartmentId))
                    {
                        departmentName = $" ({DepartmentDict[employee.DepartmentId].Name})";
                    }

                    employeeItems.Add(new SelectListItem
                    {
                        Value = employee.Id.ToString(),
                        Text = employee.FullName + departmentName
                    });
                }
            }
            EmployeeList = new SelectList(employeeItems, "Value", "Text");

            // Fetch summaries with filtering
            var allSummaries = await FetchSummariesAsync();

            // Get the total count for pagination
            TotalSummaries = allSummaries.Count;

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
            Summaries = allSummaries
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Calculate average scores
            if (allSummaries.Any())
            {
                double totalAttendanceScore = 0;
                double totalTaskScore = 0;

                foreach (var summary in allSummaries)
                {
                    totalAttendanceScore += summary.AttendanceScore;
                    totalTaskScore += summary.TaskScore;
                }

                AverageAttendanceScore = totalAttendanceScore / allSummaries.Count;
                AverageTaskScore = totalTaskScore / allSummaries.Count;
            }

            // Prepare chart data
            PrepareChartData(allSummaries);
        }

        // Handler for deleting a summary
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var summary = await _evaluationService.GetWorkActivitySummaryByIdAsync(id);
            if (summary == null)
            {
                return NotFound();
            }

            // Load employee information
            var employee = await _employeeService.GetEmployeeByIdAsync(summary.EmployeeId);
            string employeeName = employee != null ? employee.FullName : $"ID: {summary.EmployeeId}";

            var result = await _evaluationService.DeleteWorkActivitySummaryAsync(id);
            if (result)
            {
                // Log the deletion
                await _auditLogService.LogActivityAsync(
                    User.Identity.Name,
                    Models.Enums.ActionType.Delete,
                    "WorkActivitySummary",
                    id.ToString(),
                    $"Удалена сводка активности за период {summary.PeriodStart:dd.MM.yyyy} - {summary.PeriodEnd:dd.MM.yyyy} сотрудника {employeeName}"
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

        // Helper method to load all dictionaries for lookup
        private async Task LoadDictionariesAsync()
        {
            // Load all departments
            var departments = await _employeeService.GetAllDepartmentsAsync();
            DepartmentDict.Clear();
            foreach (var department in departments)
            {
                DepartmentDict[department.Id] = department;
            }

            // Load all employees
            var employees = await _employeeService.GetAllEmployeesAsync();
            EmployeeDict.Clear();
            foreach (var employee in employees)
            {
                EmployeeDict[employee.Id] = employee;
            }

            // Load all users
            var users = await _userService.GetAllUsersAsync();
            UserDict.Clear();
            foreach (var user in users)
            {
                UserDict[user.Id] = user;
            }
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
                var employees = EmployeeDict.Values.ToList();

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
            var filteredSummaries = new List<WorkActivitySummary>();
            foreach (var summary in allSummaries)
            {
                bool includeRecord = true;

                if (StartDate.HasValue && summary.PeriodEnd < StartDate.Value)
                {
                    includeRecord = false;
                }

                if (EndDate.HasValue && summary.PeriodStart > EndDate.Value)
                {
                    includeRecord = false;
                }

                if (includeRecord)
                {
                    filteredSummaries.Add(summary);
                }
            }

            // Sort by period end date (most recent first)
            filteredSummaries.Sort((a, b) => b.PeriodEnd.CompareTo(a.PeriodEnd));
            return filteredSummaries;
        }

        // Prepare chart data for the visualizations
        private void PrepareChartData(List<WorkActivitySummary> summaries)
        {
            if (summaries.Count == 0)
            {
                // No data to visualize
                return;
            }

            // Group summaries by period (month/year) for the time-based charts
            var periodGroups = new Dictionary<string, List<WorkActivitySummary>>();

            foreach (var summary in summaries)
            {
                string periodKey = $"{summary.PeriodEnd.Year}-{summary.PeriodEnd.Month:D2}";

                if (!periodGroups.ContainsKey(periodKey))
                {
                    periodGroups[periodKey] = new List<WorkActivitySummary>();
                }

                periodGroups[periodKey].Add(summary);
            }

            // Sort periods chronologically
            var sortedPeriods = periodGroups.Keys.ToList();
            sortedPeriods.Sort();

            // Prepare attendance chart data
            var labels = new List<string>();
            var attendanceDays = new List<double>();
            var lateArrivals = new List<double>();

            foreach (var period in sortedPeriods)
            {
                var periodSummaries = periodGroups[period];

                double avgAttendanceDays = 0;
                double avgLateArrivals = 0;

                foreach (var summary in periodSummaries)
                {
                    avgAttendanceDays += summary.AttendanceDays;
                    avgLateArrivals += summary.LateArrivals;
                }

                avgAttendanceDays /= periodSummaries.Count;
                avgLateArrivals /= periodSummaries.Count;

                labels.Add(period);
                attendanceDays.Add(avgAttendanceDays);
                lateArrivals.Add(avgLateArrivals);
            }

            AttendanceChartData = new
            {
                labels = labels.ToArray(),
                attendanceDays = attendanceDays.ToArray(),
                lateArrivals = lateArrivals.ToArray()
            };

            // Prepare task chart data
            var completedTasks = new List<double>();
            var efficiency = new List<double>();

            foreach (var period in sortedPeriods)
            {
                var periodSummaries = periodGroups[period];

                double avgCompletedTasks = 0;
                double avgEfficiency = 0;

                foreach (var summary in periodSummaries)
                {
                    avgCompletedTasks += summary.CompletedTasks;
                    avgEfficiency += summary.AvgTaskEfficiency;
                }

                avgCompletedTasks /= periodSummaries.Count;
                avgEfficiency /= periodSummaries.Count;

                completedTasks.Add(avgCompletedTasks);
                efficiency.Add(avgEfficiency);
            }

            TaskChartData = new
            {
                labels = labels.ToArray(),
                completedTasks = completedTasks.ToArray(),
                efficiency = efficiency.ToArray()
            };

            // Prepare department comparison chart data
            if (DepartmentId == null)
            {
                // No department selected, show all departments
                var departmentGroups = new Dictionary<string, List<WorkActivitySummary>>();

                foreach (var summary in summaries)
                {
                    string departmentName = "Неизвестный отдел";

                    if (EmployeeDict.ContainsKey(summary.EmployeeId))
                    {
                        var employee = EmployeeDict[summary.EmployeeId];
                        if (DepartmentDict.ContainsKey(employee.DepartmentId))
                        {
                            departmentName = DepartmentDict[employee.DepartmentId].Name;
                        }
                    }

                    if (!departmentGroups.ContainsKey(departmentName))
                    {
                        departmentGroups[departmentName] = new List<WorkActivitySummary>();
                    }

                    departmentGroups[departmentName].Add(summary);
                }

                var departmentNames = new List<string>();
                var attendanceScores = new List<double>();
                var taskScores = new List<double>();
                var penaltyScores = new List<double>();

                foreach (var entry in departmentGroups)
                {
                    string departmentName = entry.Key;
                    var departmentSummaries = entry.Value;

                    double avgAttendanceScore = 0;
                    double avgTaskScore = 0;
                    double avgPenaltyScore = 0;

                    foreach (var summary in departmentSummaries)
                    {
                        avgAttendanceScore += summary.AttendanceScore;
                        avgTaskScore += summary.TaskScore;
                        avgPenaltyScore += summary.PenaltyScore;
                    }

                    avgAttendanceScore /= departmentSummaries.Count;
                    avgTaskScore /= departmentSummaries.Count;
                    avgPenaltyScore /= departmentSummaries.Count;

                    departmentNames.Add(departmentName);
                    attendanceScores.Add(avgAttendanceScore);
                    taskScores.Add(avgTaskScore);
                    penaltyScores.Add(avgPenaltyScore);
                }

                DepartmentChartData = new
                {
                    departments = departmentNames.ToArray(),
                    attendanceScores = attendanceScores.ToArray(),
                    taskScores = taskScores.ToArray(),
                    penaltyScores = penaltyScores.ToArray()
                };
            }
            else
            {
                // Department selected, show employees in that department
                var employeeGroups = new Dictionary<string, List<WorkActivitySummary>>();

                foreach (var summary in summaries)
                {
                    if (EmployeeDict.ContainsKey(summary.EmployeeId))
                    {
                        var employee = EmployeeDict[summary.EmployeeId];

                        if (employee.DepartmentId == DepartmentId.Value)
                        {
                            string employeeName = employee.FullName;

                            if (!employeeGroups.ContainsKey(employeeName))
                            {
                                employeeGroups[employeeName] = new List<WorkActivitySummary>();
                            }

                            employeeGroups[employeeName].Add(summary);
                        }
                    }
                }

                var employeeNames = new List<string>();
                var attendanceScores = new List<double>();
                var taskScores = new List<double>();
                var penaltyScores = new List<double>();

                foreach (var entry in employeeGroups)
                {
                    string employeeName = entry.Key;
                    var employeeSummaries = entry.Value;

                    double avgAttendanceScore = 0;
                    double avgTaskScore = 0;
                    double avgPenaltyScore = 0;

                    foreach (var summary in employeeSummaries)
                    {
                        avgAttendanceScore += summary.AttendanceScore;
                        avgTaskScore += summary.TaskScore;
                        avgPenaltyScore += summary.PenaltyScore;
                    }

                    avgAttendanceScore /= employeeSummaries.Count;
                    avgTaskScore /= employeeSummaries.Count;
                    avgPenaltyScore /= employeeSummaries.Count;

                    employeeNames.Add(employeeName);
                    attendanceScores.Add(avgAttendanceScore);
                    taskScores.Add(avgTaskScore);
                    penaltyScores.Add(avgPenaltyScore);
                }

                DepartmentChartData = new
                {
                    departments = employeeNames.ToArray(),
                    attendanceScores = attendanceScores.ToArray(),
                    taskScores = taskScores.ToArray(),
                    penaltyScores = penaltyScores.ToArray()
                };
            }
        }
    }
}