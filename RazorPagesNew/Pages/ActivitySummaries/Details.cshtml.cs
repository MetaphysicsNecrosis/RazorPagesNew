using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;

namespace RazorPagesNew.Pages.ActivitySummaries
{
    public class DetailsModel : PageModel
    {
        private readonly MyApplicationDbContext _context;

        public DetailsModel(MyApplicationDbContext context)
        {
            _context = context;
        }

        public WorkActivitySummary ActivitySummary { get; set; }
        public string EmployeeFullName { get; set; }
        public string EmployeePosition { get; set; }
        public string DepartmentName { get; set; }
        public string OwnerUsername { get; set; }
        public List<AttendanceRecord> RelatedAttendanceRecords { get; set; }
        public List<TaskRecord> RelatedTaskRecords { get; set; }
        public List<LeaveRecord> RelatedLeaveRecords { get; set; }
        public int WorkingDaysInPeriod { get; set; }

        // ������ ��� ��������
        public Dictionary<string, double> AttendanceChartData { get; set; }
        public Dictionary<string, double> TaskEfficiencyChartData { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // �������� �������� ������ ���������� ��� ������������� �������
            ActivitySummary = await _context.WorkActivitySummaries.FindAsync(id);

            if (ActivitySummary == null)
            {
                return NotFound();
            }

            // �������� ������ � ���������� ��� ������������� �������
            var employeeData = await _context.Employees
                .Where(e => e.Id == ActivitySummary.EmployeeId)
                .Select(e => new { e.FullName, e.Position, e.DepartmentId })
                .FirstOrDefaultAsync();

            if (employeeData != null)
            {
                EmployeeFullName = employeeData.FullName;
                EmployeePosition = employeeData.Position;

                // �������� �������� ������
                DepartmentName = await _context.Departments
                    .Where(d => d.Id == employeeData.DepartmentId)
                    .Select(d => d.Name)
                    .FirstOrDefaultAsync();
            }

            // �������� ��� ������������, ���������� ������
            OwnerUsername = await _context.Users
                .Where(u => u.Id == ActivitySummary.OwnerId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync();

            // �������� ��������� ������ ������������
            RelatedAttendanceRecords = await _context.AttendanceRecords
                .Where(a => a.EmployeeId == ActivitySummary.EmployeeId &&
                           a.Date >= ActivitySummary.PeriodStart &&
                           a.Date <= ActivitySummary.PeriodEnd)
                .OrderByDescending(a => a.Date)
                .Take(10)
                .ToListAsync();

            // �������� ��������� ������ �����
            RelatedTaskRecords = await _context.TaskRecords
                .Where(t => t.EmployeeId == ActivitySummary.EmployeeId &&
                           t.CompletedAt >= ActivitySummary.PeriodStart &&
                           t.CompletedAt <= ActivitySummary.PeriodEnd)
                .OrderByDescending(t => t.CompletedAt)
                .Take(10)
                .ToListAsync();

            // �������� ��������� ������ �� ��������
            RelatedLeaveRecords = await _context.LeaveRecords
                .Where(l => l.EmployeeId == ActivitySummary.EmployeeId &&
                           ((l.StartDate >= ActivitySummary.PeriodStart && l.StartDate <= ActivitySummary.PeriodEnd) ||
                            (l.EndDate >= ActivitySummary.PeriodStart && l.EndDate <= ActivitySummary.PeriodEnd) ||
                            (l.StartDate <= ActivitySummary.PeriodStart && l.EndDate >= ActivitySummary.PeriodEnd)))
                .OrderByDescending(l => l.StartDate)
                .ToListAsync();

            // ������ ���������� ������� ���� � �������
            WorkingDaysInPeriod = CountWorkingDays(ActivitySummary.PeriodStart, ActivitySummary.PeriodEnd);

            // ���������� ������ ��� ��������
            PrepareChartData();

            return Page();
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

        private void PrepareChartData()
        {
            // ������ ��� ������� ������������
            AttendanceChartData = new Dictionary<string, double>
            {
                { "�������������", ActivitySummary.AttendanceDays },
                { "������������", WorkingDaysInPeriod - ActivitySummary.AttendanceDays },
                { "������", ActivitySummary.VacationDays },
                { "����������", ActivitySummary.SickDays }
            };

            // ������ ��� ������� ������������� �����
            TaskEfficiencyChartData = new Dictionary<string, double>
            {
                { "����������� ������", ActivitySummary.CompletedTasks },
                { "������������� �����", ActivitySummary.AvgTaskEfficiency },
                { "����� ���������� �����", ActivitySummary.TaskScore }
            };
        }

        // ��������������� ������ ��� ��������������
        public string GetLeaveTypeName(int type)
        {
            return type switch
            {
                1 => "������",
                2 => "����������",
                3 => "�����",
                4 => "����������������",
                _ => "������"
            };
        }

        public string GetImportanceLevel(int importance)
        {
            return importance switch
            {
                1 => "������",
                2 => "�������",
                3 => "�������",
                4 => "�����������",
                _ => "�� ����������"
            };
        }

        public string GetScoreClass(double score)
        {
            return score >= 70 ? "success" :
                   score >= 50 ? "warning" :
                   "danger";
        }
    }
}