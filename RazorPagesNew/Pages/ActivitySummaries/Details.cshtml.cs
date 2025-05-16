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

        // Данные для графиков
        public Dictionary<string, double> AttendanceChartData { get; set; }
        public Dictionary<string, double> TaskEfficiencyChartData { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Получаем основную сводку активности без навигационных свойств
            ActivitySummary = await _context.WorkActivitySummaries.FindAsync(id);

            if (ActivitySummary == null)
            {
                return NotFound();
            }

            // Получаем данные о сотруднике без навигационных свойств
            var employeeData = await _context.Employees
                .Where(e => e.Id == ActivitySummary.EmployeeId)
                .Select(e => new { e.FullName, e.Position, e.DepartmentId })
                .FirstOrDefaultAsync();

            if (employeeData != null)
            {
                EmployeeFullName = employeeData.FullName;
                EmployeePosition = employeeData.Position;

                // Получаем название отдела
                DepartmentName = await _context.Departments
                    .Where(d => d.Id == employeeData.DepartmentId)
                    .Select(d => d.Name)
                    .FirstOrDefaultAsync();
            }

            // Получаем имя пользователя, создавшего сводку
            OwnerUsername = await _context.Users
                .Where(u => u.Id == ActivitySummary.OwnerId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync();

            // Получаем связанные записи посещаемости
            RelatedAttendanceRecords = await _context.AttendanceRecords
                .Where(a => a.EmployeeId == ActivitySummary.EmployeeId &&
                           a.Date >= ActivitySummary.PeriodStart &&
                           a.Date <= ActivitySummary.PeriodEnd)
                .OrderByDescending(a => a.Date)
                .Take(10)
                .ToListAsync();

            // Получаем связанные записи задач
            RelatedTaskRecords = await _context.TaskRecords
                .Where(t => t.EmployeeId == ActivitySummary.EmployeeId &&
                           t.CompletedAt >= ActivitySummary.PeriodStart &&
                           t.CompletedAt <= ActivitySummary.PeriodEnd)
                .OrderByDescending(t => t.CompletedAt)
                .Take(10)
                .ToListAsync();

            // Получаем связанные записи об отпусках
            RelatedLeaveRecords = await _context.LeaveRecords
                .Where(l => l.EmployeeId == ActivitySummary.EmployeeId &&
                           ((l.StartDate >= ActivitySummary.PeriodStart && l.StartDate <= ActivitySummary.PeriodEnd) ||
                            (l.EndDate >= ActivitySummary.PeriodStart && l.EndDate <= ActivitySummary.PeriodEnd) ||
                            (l.StartDate <= ActivitySummary.PeriodStart && l.EndDate >= ActivitySummary.PeriodEnd)))
                .OrderByDescending(l => l.StartDate)
                .ToListAsync();

            // Расчет количества рабочих дней в периоде
            WorkingDaysInPeriod = CountWorkingDays(ActivitySummary.PeriodStart, ActivitySummary.PeriodEnd);

            // Подготовка данных для графиков
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
            // Данные для графика посещаемости
            AttendanceChartData = new Dictionary<string, double>
            {
                { "Присутствовал", ActivitySummary.AttendanceDays },
                { "Отсутствовал", WorkingDaysInPeriod - ActivitySummary.AttendanceDays },
                { "Отпуск", ActivitySummary.VacationDays },
                { "Больничный", ActivitySummary.SickDays }
            };

            // Данные для графика эффективности задач
            TaskEfficiencyChartData = new Dictionary<string, double>
            {
                { "Выполненные задачи", ActivitySummary.CompletedTasks },
                { "Эффективность задач", ActivitySummary.AvgTaskEfficiency },
                { "Общий показатель задач", ActivitySummary.TaskScore }
            };
        }

        // Вспомогательные методы для форматирования
        public string GetLeaveTypeName(int type)
        {
            return type switch
            {
                1 => "Отпуск",
                2 => "Больничный",
                3 => "Отгул",
                4 => "Административный",
                _ => "Другое"
            };
        }

        public string GetImportanceLevel(int importance)
        {
            return importance switch
            {
                1 => "Низкая",
                2 => "Средняя",
                3 => "Высокая",
                4 => "Критическая",
                _ => "Не определена"
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