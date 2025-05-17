using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Shared;
using NuGet.Protocol.Providers;
using RazorPagesNew.Data;
using RazorPagesNew.Models.Enums;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;

namespace RazorPagesNew.Services.Implementation
{
    public class EvaluationService : IEvaluationService
    {
        private readonly MyApplicationDbContext _context;

        public EvaluationService(MyApplicationDbContext context)
        {
            _context = context;
        }

        #region Операции с оценками сотрудников

        public async Task<IEnumerable<EmployeeEvaluation>> GetAllEvaluationsAsync()
        {
            return await _context.EmployeeEvaluations
                .Include(e => e.Employee)
                .Include(e => e.Evaluator)
                .Include(e => e.Summary)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<EmployeeEvaluation> GetEvaluationByIdAsync(int id)
        {
            return await _context.EmployeeEvaluations
                .Include(e => e.Employee)
                .Include(e => e.Evaluator)
                .Include(e => e.Summary)
                .Include(e => e.EvaluationScores)
                .ThenInclude(s => s.Criterion)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IEnumerable<EmployeeEvaluation>> GetEvaluationsByEmployeeIdAsync(int employeeId)
        {
            return await _context.EmployeeEvaluations
                .Where(e => e.EmployeeId == employeeId)
                .Include(e => e.Evaluator)
                .Include(e => e.Summary)
                .Include(e => e.EvaluationScores)
                .ThenInclude(s => s.Criterion)
                .OrderByDescending(e => e.EvaluationDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<EmployeeEvaluation> CreateEvaluationAsync(EmployeeEvaluation evaluation)
        {
            if (evaluation == null)
                throw new ArgumentNullException(nameof(evaluation));

            await _context.EmployeeEvaluations.AddAsync(evaluation);
            await _context.SaveChangesAsync();

            return evaluation;
        }

        public async Task<EmployeeEvaluation> UpdateEvaluationAsync(EmployeeEvaluation evaluation)
        {
            if (evaluation == null)
                throw new ArgumentNullException(nameof(evaluation));

            var existingEvaluation = await _context.EmployeeEvaluations
                .Include(e => e.EvaluationScores)
                .FirstOrDefaultAsync(e => e.Id == evaluation.Id);

            if (existingEvaluation == null)
                throw new KeyNotFoundException($"Evaluation with ID {evaluation.Id} not found.");

            _context.Entry(existingEvaluation).CurrentValues.SetValues(evaluation);

            // Обновляем связанные оценки по критериям
            foreach (var score in evaluation.EvaluationScores)
            {
                var existingScore = existingEvaluation.EvaluationScores
                    .FirstOrDefault(s => s.Id == score.Id);

                if (existingScore != null)
                {
                    _context.Entry(existingScore).CurrentValues.SetValues(score);
                }
                else
                {
                    existingEvaluation.EvaluationScores.Add(score);
                }
            }

            // Удаляем оценки, которых нет в обновлённом списке
            foreach (var existingScore in existingEvaluation.EvaluationScores.ToList())
            {
                if (!evaluation.EvaluationScores.Any(s => s.Id == existingScore.Id))
                {
                    _context.EvaluationScores.Remove(existingScore);
                }
            }

            await _context.SaveChangesAsync();

            return existingEvaluation;
        }

        public async Task<bool> DeleteEvaluationAsync(int id)
        {
            {
                try
                {
                    // Сначала удаляем связанные оценки по критериям
                    var relatedScores = await _context.EvaluationScores
                        .Where(s => s.EvaluationId == id)
                        .ToListAsync();

                    if (relatedScores.Any())
                    {
                        _context.EvaluationScores.RemoveRange(relatedScores);
                        await _context.SaveChangesAsync();
                    }

                    // Теперь удаляем саму оценку
                    var evaluation = await _context.EmployeeEvaluations.FindAsync(id);
                    if (evaluation != null)
                    {
                        _context.EmployeeEvaluations.Remove(evaluation);
                        await _context.SaveChangesAsync();
                    }

                }
                catch (Exception ex)
                { Console.WriteLine(ex.Message); }
            }


            return true;
        }

        #endregion

        #region Операции с критериями оценки

        public async Task<IEnumerable<EvaluationCriterion>> GetAllCriteriaAsync()
        {
            return await _context.EvaluationCriteria
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<EvaluationCriterion> GetCriterionByIdAsync(int id)
        {
            return await _context.EvaluationCriteria
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<EvaluationCriterion> CreateCriterionAsync(EvaluationCriterion criterion)
        {
            if (criterion == null)
                throw new ArgumentNullException(nameof(criterion));

            await _context.EvaluationCriteria.AddAsync(criterion);
            await _context.SaveChangesAsync();

            return criterion;
        }

        public async Task<EvaluationCriterion> UpdateCriterionAsync(EvaluationCriterion criterion)
        {
            if (criterion == null)
                throw new ArgumentNullException(nameof(criterion));

            var existingCriterion = await _context.EvaluationCriteria
                .FirstOrDefaultAsync(c => c.Id == criterion.Id);

            if (existingCriterion == null)
                throw new KeyNotFoundException($"Criterion with ID {criterion.Id} not found.");

            _context.Entry(existingCriterion).CurrentValues.SetValues(criterion);
            await _context.SaveChangesAsync();

            return existingCriterion;
        }

        public async Task<bool> DeleteCriterionAsync(int id)
        {
            var criterion = await _context.EvaluationCriteria.FindAsync(id);
            if (criterion == null)
                return false;

            _context.EvaluationCriteria.Remove(criterion);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Операции с оценками по критериям

        public async Task<IEnumerable<EvaluationScore>> GetScoresByEvaluationIdAsync(int evaluationId)
        {
            return await _context.EvaluationScores
                .Where(s => s.EvaluationId == evaluationId)
                .Include(s => s.Criterion)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<EvaluationScore> GetScoreByIdAsync(int id)
        {
            return await _context.EvaluationScores
                .Include(s => s.Criterion)
                .Include(s => s.Evaluation)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<EvaluationScore> CreateScoreAsync(EvaluationScore score)
        {
            if (score == null)
                throw new ArgumentNullException(nameof(score));

            await _context.EvaluationScores.AddAsync(score);
            await _context.SaveChangesAsync();

            return score;
        }

        public async Task<EvaluationScore> UpdateScoreAsync(EvaluationScore score)
        {
            if (score == null)
                throw new ArgumentNullException(nameof(score));

            var existingScore = await _context.EvaluationScores
                .FirstOrDefaultAsync(s => s.Id == score.Id);

            if (existingScore == null)
                throw new KeyNotFoundException($"Score with ID {score.Id} not found.");

            _context.Entry(existingScore).CurrentValues.SetValues(score);
            await _context.SaveChangesAsync();

            return existingScore;
        }

        public async Task<bool> DeleteScoreAsync(int id)
        {
            var score = await _context.EvaluationScores.FindAsync(id);
            if (score == null)
                return false;

            _context.EvaluationScores.Remove(score);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Операции со сводками активности

        public async Task<WorkActivitySummary> GetWorkActivitySummaryByIdAsync(int id)
        {
            return await _context.WorkActivitySummaries
                .Include(w => w.Employee)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<IEnumerable<WorkActivitySummary>> GetWorkActivitySummariesByEmployeeIdAsync(int employeeId)
        {
            return await _context.WorkActivitySummaries
                .Where(w => w.EmployeeId == employeeId)
                .OrderByDescending(w => w.PeriodEnd)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<WorkActivitySummary> CreateWorkActivitySummaryAsync(WorkActivitySummary summary)
        {
            if (summary == null)
                throw new ArgumentNullException(nameof(summary));

            await _context.WorkActivitySummaries.AddAsync(summary);
            await _context.SaveChangesAsync();

            return summary;
        }

        public async Task<WorkActivitySummary> UpdateWorkActivitySummaryAsync(WorkActivitySummary summary)
        {
            if (summary == null)
                throw new ArgumentNullException(nameof(summary));

            var existingSummary = await _context.WorkActivitySummaries
                .FirstOrDefaultAsync(w => w.Id == summary.Id);

            if (existingSummary == null)
                throw new KeyNotFoundException($"Work activity summary with ID {summary.Id} not found.");

            _context.Entry(existingSummary).CurrentValues.SetValues(summary);
            await _context.SaveChangesAsync();

            return existingSummary;
        }

        public async Task<bool> DeleteWorkActivitySummaryAsync(int id)
        {
            var summary = await _context.WorkActivitySummaries.FindAsync(id);
            if (summary == null)
                return false;

            _context.WorkActivitySummaries.Remove(summary);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Расчеты и аналитика

        public async Task<WorkActivitySummary> GenerateWorkActivitySummaryAsync(int employeeId, DateTime startDate, DateTime endDate, int ownerId)
        {
            // Загружаем необходимые данные для расчета
            var attendanceRecords = await _context.AttendanceRecords
                .Where(a => a.EmployeeId == employeeId && a.Date >= startDate && a.Date <= endDate)
                .ToListAsync();

            var leaveRecords = await _context.LeaveRecords
                .Where(l => l.EmployeeId == employeeId &&
                       ((l.StartDate >= startDate && l.StartDate <= endDate) ||
                        (l.EndDate >= startDate && l.EndDate <= endDate) ||
                        (l.StartDate <= startDate && l.EndDate >= endDate)))
                .ToListAsync();

            var taskRecords = await _context.TaskRecords
                .Where(t => t.EmployeeId == employeeId && t.CompletedAt >= startDate && t.CompletedAt <= endDate)
                .ToListAsync();

            // Создаем новую сводку
            var summary = new WorkActivitySummary
            {
                EmployeeId = employeeId,
                PeriodStart = startDate,
                PeriodEnd = endDate,

                // Рассчитываем посещаемость
                AttendanceDays = attendanceRecords.Count,
                LateArrivals = attendanceRecords.Count(a => a.CheckIn.TimeOfDay > new TimeSpan(9, 0, 0)), // Пример: считаем опозданием приход после 9:00
                TotalHoursWorked = attendanceRecords.Sum(a => a.HoursWorked),

                // Рассчитываем отпуска
                TotalLeaveDays = leaveRecords.Sum(l => l.DayCount),
                SickDays = leaveRecords.Where(l => l.Type == (int)LeaveType.SickLeave).Sum(l => l.DayCount),
                VacationDays = leaveRecords.Where(l => l.Type == ((int)LeaveType.Vacation)).Sum(l => l.DayCount),

                // Рассчитываем задачи
                CompletedTasks = taskRecords.Count,
                AvgTaskEfficiency = taskRecords.Any(t => t.EfficiencyScore.HasValue)
                    ? taskRecords.Where(t => t.EfficiencyScore.HasValue).Average(t => t.EfficiencyScore.Value)
                    : 0,
                OwnerId = ownerId
            };

            // Рассчитываем оценочные показатели
            int workingDaysInPeriod = CountWorkingDays(startDate, endDate);

            // Оценка посещаемости (процент от рабочих дней)
            summary.AttendanceScore = workingDaysInPeriod > 0
                ? Math.Min((double)summary.AttendanceDays / workingDaysInPeriod * 100, 100)
                : 0;

            // Оценка выполнения задач (на основе эффективности)
            summary.TaskScore = summary.AvgTaskEfficiency;

            // Штрафные баллы (за опоздания)
            summary.PenaltyScore = summary.AttendanceDays > 0
                ? Math.Max(0, 100 - ((double)summary.LateArrivals / summary.AttendanceDays * 100))
                : 0;

            return summary;
        }

        public async Task<double> CalculateOverallScoreAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var now = DateTime.UtcNow;
            var actualStartDate = startDate ?? now.AddMonths(-3);
            var actualEndDate = endDate ?? now;

            // Находим сводки за указанный период
            var summaries = await _context.WorkActivitySummaries
                .Where(w => w.EmployeeId == employeeId &&
                       w.PeriodStart >= actualStartDate &&
                       w.PeriodEnd <= actualEndDate)
                .ToListAsync();

            if (!summaries.Any())
                return 0;

            // Находим оценки за указанный период
            var evaluations = await _context.EmployeeEvaluations
                .Where(e => e.EmployeeId == employeeId &&
                       e.EvaluationDate >= actualStartDate &&
                       e.EvaluationDate <= actualEndDate)
                .ToListAsync();

            // Рассчитываем среднее значение
            double overallScore = 0;
            double weight = 0;

            // Учитываем сводки активности (50% веса)
            if (summaries.Any())
            {
                double avgAttendance = summaries.Average(s => s.AttendanceScore);
                double avgTask = summaries.Average(s => s.TaskScore);
                double avgPenalty = summaries.Average(s => s.PenaltyScore);

                // Формула для сводной оценки активности (пример)
                double activityScore = (avgAttendance * 0.4) + (avgTask * 0.4) + (avgPenalty * 0.2);

                overallScore += activityScore * 0.5;
                weight += 0.5;
            }

            // Учитываем оценки руководителей (50% веса)
            if (evaluations.Any())
            {
                double avgEvaluation = evaluations.Average(e => e.Score);

                overallScore += avgEvaluation * 0.5;
                weight += 0.5;
            }

            // Нормализуем результат с учетом весов
            return weight > 0 ? overallScore / weight : 0;
        }

        #endregion

        #region Вспомогательные методы

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

        #endregion
    }
}
