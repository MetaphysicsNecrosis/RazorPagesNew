using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.ActivitySummaries
{
    public class DetailsModel : PageModel
    {
        private readonly IEvaluationService _evaluationService;
        private readonly IEmployeeService _employeeService;
        private readonly IAuditLogService _auditLogService;

        public DetailsModel(
            IEvaluationService evaluationService,
            IEmployeeService employeeService,
            IAuditLogService auditLogService)
        {
            _evaluationService = evaluationService;
            _employeeService = employeeService;
            _auditLogService = auditLogService;
        }

        public WorkActivitySummary Summary { get; set; }
        public IEnumerable<EmployeeEvaluation> RelatedEvaluations { get; set; }
        public int WorkingDaysInPeriod { get; set; }
        
        // Overall score calculated from the summary
        public double OverallScore { get; set; }
        
        // Department statistics for comparison
        public double DepartmentAvgAttendanceDays { get; set; }
        public int DepartmentMaxAttendanceDays { get; set; }
        public double DepartmentAvgLateArrivals { get; set; }
        public int DepartmentMinLateArrivals { get; set; }
        public double DepartmentAvgHoursWorked { get; set; }
        public double DepartmentMaxHoursWorked { get; set; }
        
        public double DepartmentAvgCompletedTasks { get; set; }
        public int DepartmentMaxCompletedTasks { get; set; }
        public double DepartmentAvgTaskEfficiency { get; set; }
        public double DepartmentMaxTaskEfficiency { get; set; }
        public double DepartmentAvgTaskScore { get; set; }
        public double DepartmentMaxTaskScore { get; set; }
        
        // Chart data for employee performance trends
        public object PerformanceChartData { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Get summary details
            Summary = await _evaluationService.GetWorkActivitySummaryByIdAsync(id);
            if (Summary == null)
            {
                return NotFound();
            }

            // Calculate overall score
            OverallScore = (Summary.AttendanceScore * 0.4) + (Summary.TaskScore * 0.4) + (Summary.PenaltyScore * 0.2);

            // Calculate working days in period
            WorkingDaysInPeriod = CountWorkingDays(Summary.PeriodStart, Summary.PeriodEnd);

            // Get related evaluations
            var allEvaluations = await _evaluationService.GetEvaluationsByEmployeeIdAsync(Summary.EmployeeId);
            RelatedEvaluations = allEvaluations
                .Where(e => e.SummaryId == Summary.Id)
                .OrderByDescending(e => e.EvaluationDate)
                .ToList();

            // Load department statistics for comparison
            await LoadDepartmentStatistics();

            // Prepare chart data for performance trends
            await PreparePerformanceChartData();

            return Page();
        }

        private async Task LoadDepartmentStatistics()
        {
            // Get department ID of the employee
            var departmentId = Summary.Employee.DepartmentId;

            // Get all employees in the same department
            var departmentEmployees = await _employeeService.GetEmployeesByDepartmentAsync(departmentId);
            
            // Get summaries for all department employees in a similar period
            var departmentSummaries = new List<WorkActivitySummary>();
            foreach (var employee in departmentEmployees)
            {
                if (employee.Id == Summary.EmployeeId)
                    continue; // Skip the current employee

                var employeeSummaries = await _evaluationService.GetWorkActivitySummariesByEmployeeIdAsync(employee.Id);
                
                // Find summaries with overlapping periods
                var relevantSummaries = employeeSummaries
                    .Where(s => (s.PeriodStart <= Summary.PeriodEnd && s.PeriodEnd >= Summary.PeriodStart) ||
                               (s.PeriodStart >= Summary.PeriodStart && s.PeriodStart <= Summary.PeriodEnd) ||
                               (s.PeriodEnd >= Summary.PeriodStart && s.PeriodEnd <= Summary.PeriodEnd))
                    .ToList();
                
                departmentSummaries.AddRange(relevantSummaries);
            }

            // Calculate attendance statistics
            if (departmentSummaries.Any())
            {
                // Attendance days
                DepartmentAvgAttendanceDays = departmentSummaries.Average(s => s.AttendanceDays);
                DepartmentMaxAttendanceDays = departmentSummaries.Max(s => s.AttendanceDays);

                // Late arrivals (lower is better)
                DepartmentAvgLateArrivals = departmentSummaries.Average(s => s.LateArrivals);
                DepartmentMinLateArrivals = departmentSummaries.Min(s => s.LateArrivals);

                // Hours worked
                DepartmentAvgHoursWorked = departmentSummaries.Average(s => s.TotalHoursWorked);
                DepartmentMaxHoursWorked = departmentSummaries.Max(s => s.TotalHoursWorked);

                // Task statistics
                DepartmentAvgCompletedTasks = departmentSummaries.Average(s => s.CompletedTasks);
                DepartmentMaxCompletedTasks = departmentSummaries.Max(s => s.CompletedTasks);
                DepartmentAvgTaskEfficiency = departmentSummaries.Average(s => s.AvgTaskEfficiency);
                DepartmentMaxTaskEfficiency = departmentSummaries.Max(s => s.AvgTaskEfficiency);
                DepartmentAvgTaskScore = departmentSummaries.Average(s => s.TaskScore);
                DepartmentMaxTaskScore = departmentSummaries.Max(s => s.TaskScore);
            }
            else
            {
                // If no other employees in department or no relevant summaries,
                // set comparison values to this employee's values
                DepartmentAvgAttendanceDays = Summary.AttendanceDays;
                DepartmentMaxAttendanceDays = Summary.AttendanceDays;
                DepartmentAvgLateArrivals = Summary.LateArrivals;
                DepartmentMinLateArrivals = Summary.LateArrivals;
                DepartmentAvgHoursWorked = Summary.TotalHoursWorked;
                DepartmentMaxHoursWorked = Summary.TotalHoursWorked;
                DepartmentAvgCompletedTasks = Summary.CompletedTasks;
                DepartmentMaxCompletedTasks = Summary.CompletedTasks;
                DepartmentAvgTaskEfficiency = Summary.AvgTaskEfficiency;
                DepartmentMaxTaskEfficiency = Summary.AvgTaskEfficiency;
                DepartmentAvgTaskScore = Summary.TaskScore;
                DepartmentMaxTaskScore = Summary.TaskScore;
            }
        }

        private async Task PreparePerformanceChartData()
        {
            // Get all summaries for this employee to show trends
            var allSummaries = await _evaluationService.GetWorkActivitySummariesByEmployeeIdAsync(Summary.EmployeeId);
            
            // Order by period end date
            var orderedSummaries = allSummaries
                .OrderBy(s => s.PeriodEnd)
                .ToList();

            if (orderedSummaries.Count <= 1)
            {
                // Not enough data for a trend chart, use current summary as a single data point
                PerformanceChartData = new
                {
                    labels = new[] { $"{Summary.PeriodStart:MM/yyyy} - {Summary.PeriodEnd:MM/yyyy}" },
                    attendanceScores = new[] { Summary.AttendanceScore },
                    taskScores = new[] { Summary.TaskScore },
                    overallScores = new[] { OverallScore }
                };
                return;
            }

            // Prepare chart data
            var labels = new List<string>();
            var attendanceScores = new List<double>();
            var taskScores = new List<double>();
            var overallScores = new List<double>();

            foreach (var summary in orderedSummaries)
            {
                labels.Add($"{summary.PeriodStart:MM/yyyy} - {summary.PeriodEnd:MM/yyyy}");
                attendanceScores.Add(summary.AttendanceScore);
                taskScores.Add(summary.TaskScore);
                
                // Calculate overall score for each summary
                var overall = (summary.AttendanceScore * 0.4) + (summary.TaskScore * 0.4) + (summary.PenaltyScore * 0.2);
                overallScores.Add(overall);
            }

            PerformanceChartData = new
            {
                labels = labels.ToArray(),
                attendanceScores = attendanceScores.ToArray(),
                taskScores = taskScores.ToArray(),
                overallScores = overallScores.ToArray()
            };
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