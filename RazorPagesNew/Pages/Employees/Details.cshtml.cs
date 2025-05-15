using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Employees
{
    public class DetailsModel : PageModel
    {
        private readonly IEmployeeService _employeeService;
        private readonly IEvaluationService _evaluationService;

        public DetailsModel(IEmployeeService employeeService, IEvaluationService evaluationService)
        {
            _employeeService = employeeService;
            _evaluationService = evaluationService;
        }

        public Employee Employee { get; set; }
        public List<EmployeeEvaluation> Evaluations { get; set; } = new();
        public List<AttendanceRecord> AttendanceRecords { get; set; } = new();
        public List<LeaveRecord> LeaveRecords { get; set; } = new();
        public double OverallScore { get; set; }
        public int CompletedTasksCount { get; set; }
        public PerformanceChartData PerformanceChartData1 { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // �������� ���������� �� ID
            Employee = await _employeeService.GetEmployeeByIdAsync(id);

            if (Employee == null)
            {
                return NotFound();
            }

            // �������� ������ ����������
            Evaluations = (await _evaluationService.GetEvaluationsByEmployeeIdAsync(id)).ToList();

            // �������� ������ � ������������
            AttendanceRecords = (await _employeeService.GetEmployeeAttendanceAsync(id, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow)).ToList();

            // �������� ������ �� ��������
            LeaveRecords = (await _employeeService.GetEmployeeLeaveRecordsAsync(id, DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow)).ToList();

            // ����� ������ ����������
            OverallScore = await _evaluationService.CalculateOverallScoreAsync(id);

            // ���������� ����������� �����
            CompletedTasksCount = await _employeeService.GetEmployeeCompletedTasksCountAsync(id, DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow);

            // ���������� ������ ��� �������
            await PrepareChartData(id);

            return Page();
        }

        private async Task PrepareChartData(int employeeId)
        {
            // �������������� ������ �� ��������� 6 �������
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMonths(-6);

            // �������� ������ ���������� �� ������
            var summaries = await _evaluationService.GetWorkActivitySummariesByEmployeeIdAsync(employeeId);
            var filteredSummaries = summaries.Where(s => s.PeriodEnd >= startDate && s.PeriodStart <= endDate).OrderBy(s => s.PeriodStart).ToList();

            // ���������� ������ ��� �������
            var labels = new List<string>();
            var scores = new List<double>();
            var attendance = new List<double>();
            var tasks = new List<double>();

            foreach (var summary in filteredSummaries)
            {
                labels.Add(summary.PeriodEnd.ToString("MM/yyyy"));
                scores.Add(Math.Round((summary.AttendanceScore * 0.3) + (summary.TaskScore * 0.5) + (summary.PenaltyScore * 0.2), 1));
                attendance.Add(Math.Round(summary.AttendanceScore, 1));
                tasks.Add(Math.Round(summary.TaskScore, 1));
            }

            PerformanceChartData1 = new PerformanceChartData
            {
                Labels = labels,
                Scores = scores,
                Attendance = attendance,
                Tasks = tasks
            };
        }

        public string GetEmploymentTypeName(int employmentType)
        {
            return employmentType switch
            {
                1 => "������ ���������",
                2 => "��������� ���������",
                3 => "��������",
                4 => "����������",
                _ => "�� �������"
            };
        }

        public class PerformanceChartData
        {
            public List<string> Labels { get; set; } = new();
            public List<double> Scores { get; set; } = new();
            public List<double> Attendance { get; set; } = new();
            public List<double> Tasks { get; set; } = new();
        }
    }
}