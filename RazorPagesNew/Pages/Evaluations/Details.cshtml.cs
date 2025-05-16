using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Evaluations
{
    public class DetailsModel : PageModel
    {
        private readonly MyApplicationDbContext _context;

        public DetailsModel(MyApplicationDbContext context)
        {
            _context = context;
        }

        // Основная модель для страницы без использования навигационных свойств
        public class EvaluationDetailsViewModel
        {
            // Основная информация об оценке
            public int Id { get; set; }
            public DateTime EvaluationDate { get; set; }
            public double Score { get; set; }
            public string Notes { get; set; }

            // Информация о сотруднике
            public int EmployeeId { get; set; }
            public string EmployeeFullName { get; set; }
            public string EmployeePosition { get; set; }
            public string EmployeeEmail { get; set; }
            public string EmployeePhone { get; set; }
            public string PhotoPath { get; set; }
            public DateTime EmployeeHireDate { get; set; }

            // Информация об отделе
            public int DepartmentId { get; set; }
            public string DepartmentName { get; set; }

            // Информация об оценивающем
            public int EvaluatorId { get; set; }
            public string EvaluatorUsername { get; set; }

            // Информация о сводке активности
            public int SummaryId { get; set; }
            public DateTime PeriodStart { get; set; }
            public DateTime PeriodEnd { get; set; }
            public int AttendanceDays { get; set; }
            public int LateArrivals { get; set; }
            public double TotalHoursWorked { get; set; }
            public int CompletedTasks { get; set; }
            public double AvgTaskEfficiency { get; set; }
            public double AttendanceScore { get; set; }
            public double TaskScore { get; set; }
            public double PenaltyScore { get; set; }
        }

        // Модель для отображения оценок по критериям
        public class CriterionScoreViewModel
        {
            public int Id { get; set; }
            public int CriterionId { get; set; }
            public string CriterionName { get; set; }
            public double Weight { get; set; }
            public double Score { get; set; }
            public double WeightedScore => Score * Weight;
        }

        public EvaluationDetailsViewModel Evaluation { get; set; }
        public List<CriterionScoreViewModel> EvaluationScores { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Получаем базовую информацию об оценке
            var evaluation = await _context.EmployeeEvaluations.FindAsync(id);
            if (evaluation == null)
            {
                return NotFound();
            }

            // Получаем информацию о сотруднике
            var employee = await _context.Employees
                .Where(e => e.Id == evaluation.EmployeeId)
                .Select(e => new
                {
                    e.Id,
                    e.FullName,
                    e.Position,
                    e.Email,
                    e.Phone,
                    e.PhotoPath,
                    e.HireDate,
                    e.DepartmentId
                })
                .FirstOrDefaultAsync();

            if (employee == null)
            {
                return NotFound("Сотрудник не найден");
            }

            // Получаем информацию об отделе
            var department = await _context.Departments
                .Where(d => d.Id == employee.DepartmentId)
                .Select(d => new { d.Id, d.Name })
                .FirstOrDefaultAsync();

            // Получаем информацию об оценивающем
            var evaluator = await _context.Users
                .Where(u => u.Id == evaluation.EvaluatorId)
                .Select(u => new { u.Id, u.Username })
                .FirstOrDefaultAsync();

            // Получаем информацию о сводке активности
            var summary = await _context.WorkActivitySummaries
                .Where(s => s.Id == evaluation.SummaryId)
                .Select(s => new
                {
                    s.Id,
                    s.PeriodStart,
                    s.PeriodEnd,
                    s.AttendanceDays,
                    s.LateArrivals,
                    s.TotalHoursWorked,
                    s.CompletedTasks,
                    s.AvgTaskEfficiency,
                    s.AttendanceScore,
                    s.TaskScore,
                    s.PenaltyScore
                })
                .FirstOrDefaultAsync();

            // Создаем модель представления для оценки
            Evaluation = new EvaluationDetailsViewModel
            {
                Id = evaluation.Id,
                EvaluationDate = evaluation.EvaluationDate,
                Score = evaluation.Score,
                Notes = evaluation.Notes,

                // Данные о сотруднике
                EmployeeId = employee.Id,
                EmployeeFullName = employee.FullName,
                EmployeePosition = employee.Position,
                EmployeeEmail = employee.Email,
                EmployeePhone = employee.Phone,
                PhotoPath = employee.PhotoPath,
                EmployeeHireDate = employee.HireDate,

                // Данные об отделе
                DepartmentId = department?.Id ?? 0,
                DepartmentName = department?.Name ?? "Не указан",

                // Данные об оценивающем
                EvaluatorId = evaluator?.Id ?? 0,
                EvaluatorUsername = evaluator?.Username ?? "Не указан",

                // Данные о сводке активности
                SummaryId = summary?.Id ?? 0
            };

            // Заполняем данные о сводке активности, если она существует
            if (summary != null)
            {
                Evaluation.PeriodStart = summary.PeriodStart;
                Evaluation.PeriodEnd = summary.PeriodEnd;
                Evaluation.AttendanceDays = summary.AttendanceDays;
                Evaluation.LateArrivals = summary.LateArrivals;
                Evaluation.TotalHoursWorked = summary.TotalHoursWorked;
                Evaluation.CompletedTasks = summary.CompletedTasks;
                Evaluation.AvgTaskEfficiency = summary.AvgTaskEfficiency;
                Evaluation.AttendanceScore = summary.AttendanceScore;
                Evaluation.TaskScore = summary.TaskScore;
                Evaluation.PenaltyScore = summary.PenaltyScore;
            }

            // Получаем оценки по критериям без использования навигационных свойств
            EvaluationScores = new List<CriterionScoreViewModel>();

            var scores = await _context.EvaluationScores
                .Where(s => s.EvaluationId == evaluation.Id)
                .Select(s => new
                {
                    s.Id,
                    s.CriterionId,
                    s.Score
                })
                .ToListAsync();

            foreach (var score in scores)
            {
                // Получаем информацию о критерии
                var criterion = await _context.EvaluationCriteria
                    .Where(c => c.Id == score.CriterionId)
                    .Select(c => new
                    {
                        c.Name,
                        c.Weight
                    })
                    .FirstOrDefaultAsync();

                if (criterion != null)
                {
                    EvaluationScores.Add(new CriterionScoreViewModel
                    {
                        Id = score.Id,
                        CriterionId = score.CriterionId,
                        CriterionName = criterion.Name,
                        Weight = criterion.Weight,
                        Score = score.Score
                    });
                }
            }

            return Page();
        }

        // Метод для определения CSS класса на основе значения оценки
        public string GetScoreClass(double score)
        {
            if (score >= 70) return "success";
            if (score >= 50) return "warning";
            return "danger";
        }

        // Метод для описания результата оценки на основе её значения
        public string GetScoreDescription(double score)
        {
            if (score >= 90) return "Превосходно";
            if (score >= 80) return "Отлично";
            if (score >= 70) return "Хорошо";
            if (score >= 60) return "Выше среднего";
            if (score >= 50) return "Удовлетворительно";
            if (score >= 40) return "Ниже среднего";
            if (score >= 30) return "Требует улучшения";
            return "Неудовлетворительно";
        }
    }
}