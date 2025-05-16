using RazorPagesNew.ModelsDb;
using RazorPagesNew.ModelsDb.Reports;
using RazorPagesNew.Services.Interfaces;
using System.Text.Json;

namespace RazorPagesNew.Services.Implementation
{
    /// <summary>
    /// Реализация сервиса отчетов
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IEmployeeService _employeeService;
        private readonly IEvaluationService _evaluationService;
        private readonly IUserService _userService;
        private readonly string _reportsDirectory;

        public ReportService(
            IEmployeeService employeeService,
            IEvaluationService evaluationService,
            IUserService userService)
        {
            _employeeService = employeeService;
            _evaluationService = evaluationService;
            _userService = userService;
            _reportsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");

            // Создаем директорию для хранения отчетов, если она не существует
            if (!Directory.Exists(_reportsDirectory))
            {
                Directory.CreateDirectory(_reportsDirectory);
            }
        }

        /// <summary>
        /// Генерация индивидуального отчета по сотруднику
        /// </summary>
        public async Task<EmployeeReport> GenerateEmployeeReportAsync(int employeeId, DateTime startDate, DateTime endDate)
        {
            // Получение данных сотрудника
            var employee = await _employeeService.GetEmployeeByIdAsync(employeeId);
            if (employee == null)
            {
                throw new ArgumentException($"Employee with ID {employeeId} not found");
            }

            // Получение оценок сотрудника
            var evaluations = await _evaluationService.GetEvaluationsByEmployeeIdAsync(employeeId);
            evaluations = evaluations.Where(e => e.EvaluationDate >= startDate && e.EvaluationDate <= endDate).ToList();

            // Получение активности сотрудника
            var activitySummaries = await _evaluationService.GetWorkActivitySummariesByEmployeeIdAsync(employeeId);
            activitySummaries = activitySummaries.Where(a => a.PeriodEnd >= startDate && a.PeriodStart <= endDate).ToList();

            // Получение посещаемости
            var attendanceRecords = await _employeeService.GetEmployeeAttendanceAsync(employeeId, startDate, endDate);

            // Получение задач
            var taskRecords = await _employeeService.GetEmployeeTasksAsync(employeeId, startDate, endDate);

            // Получение отпусков
            var leaveRecords = await _employeeService.GetEmployeeLeaveRecordsAsync(employeeId, startDate, endDate);

            // Создание отчета
            var report = new EmployeeReport
            {
                Title = $"Employee Report: {employee.FullName} ({startDate.ToString("dd.MM.yyyy")} - {endDate.ToString("dd.MM.yyyy")})",
                GeneratedAt = DateTime.Now,
                OwnerId = employee.OwnerId,
                Employee = employee,
                Evaluations = evaluations.ToList(),
                ActivitySummaries = activitySummaries.ToList()
            };

            // Расчет среднего балла
            if (evaluations.Any())
            {
                report.AverageScore = evaluations.Average(e => e.Score);
            }

            // Формирование статистики посещаемости
            report.AttendanceStats = CalculateAttendanceStatistics(attendanceRecords.ToList(), leaveRecords.ToList(), startDate, endDate);

            // Формирование статистики задач
            report.TaskStats = CalculateTaskStatistics(taskRecords.ToList(), startDate, endDate);

            // Формирование оценок по критериям
            report.CriteriaScores = await CalculateCriteriaScoresAsync(employeeId, evaluations.ToList());

            return report;
        }

        /// <summary>
        /// Генерация отчета по отделу
        /// </summary>
        public async Task<DepartmentReport> GenerateDepartmentReportAsync(int departmentId, DateTime startDate, DateTime endDate)
        {
            // Получение данных отдела
            var departments = await _employeeService.GetAllDepartmentsAsync();
            var department = departments.FirstOrDefault(d => d.Id == departmentId);
            if (department == null)
            {
                throw new ArgumentException($"Department with ID {departmentId} not found");
            }

            // Получение сотрудников отдела
            var employees = await _employeeService.GetEmployeesByDepartmentAsync(departmentId);

            // Создание отчета
            var report = new DepartmentReport
            {
                Title = $"Department Report: {department.Name} ({startDate.ToString("dd.MM.yyyy")} - {endDate.ToString("dd.MM.yyyy")})",
                GeneratedAt = DateTime.Now,
                OwnerId = 1, // Здесь должен быть ID текущего пользователя
                Department = department
            };

            // Формирование рейтинга сотрудников
            var employeeRatings = new List<EmployeeRating>();
            double departmentScore = 0;
            double departmentAttendanceRate = 0;
            int departmentCompletedTasks = 0;

            foreach (var employee in employees)
            {
                // Получение оценок сотрудника
                var evaluations = await _evaluationService.GetEvaluationsByEmployeeIdAsync(employee.Id);
                evaluations = evaluations.Where(e => e.EvaluationDate >= startDate && e.EvaluationDate <= endDate).ToList();

                // Получение задач сотрудника
                var taskRecords = await _employeeService.GetEmployeeTasksAsync(employee.Id, startDate, endDate);

                // Получение посещаемости
                var attendanceRecords = await _employeeService.GetEmployeeAttendanceAsync(employee.Id, startDate, endDate);

                // Получение отпусков
                var leaveRecords = await _employeeService.GetEmployeeLeaveRecordsAsync(employee.Id, startDate, endDate);

                // Расчет среднего балла
                double averageScore = 0;
                if (evaluations.Any())
                {
                    averageScore = evaluations.Average(e => e.Score);
                }

                // Расчет посещаемости
                var attendanceStats = CalculateAttendanceStatistics(attendanceRecords.ToList(), leaveRecords.ToList(), startDate, endDate);

                // Добавление в рейтинг
                employeeRatings.Add(new EmployeeRating
                {
                    Employee = employee,
                    AverageScore = averageScore,
                    CompletedTasks = taskRecords.Count(),
                    AttendanceRate = attendanceStats.AttendanceRate
                });

                // Суммирование для отдела
                departmentScore += averageScore;
                departmentAttendanceRate += attendanceStats.AttendanceRate;
                departmentCompletedTasks += taskRecords.Count();
            }

            // Расчет средних значений для отдела
            if (employees.Any())
            {
                report.DepartmentAverageScore = departmentScore / employees.Count();
                departmentAttendanceRate /= employees.Count();
            }

            // Ранжирование сотрудников
            employeeRatings = employeeRatings.OrderByDescending(e => e.AverageScore).ToList();
            for (int i = 0; i < employeeRatings.Count; i++)
            {
                employeeRatings[i].Rank = i + 1;
            }

            report.EmployeeRatings = employeeRatings;

            // Формирование статистики посещаемости отдела
            report.DepartmentAttendanceStats = new AttendanceStatistics
            {
                AttendanceRate = departmentAttendanceRate
                // Остальные поля будут заполнены в реальной реализации
            };

            // Формирование статистики задач отдела
            report.DepartmentTaskStats = new TaskStatistics
            {
                CompletedTasks = departmentCompletedTasks
                // Остальные поля будут заполнены в реальной реализации
            };

            return report;
        }

        /// <summary>
        /// Генерация отчета по критериям оценки
        /// </summary>
        public async Task<CriteriaAnalysisReport> GenerateCriteriaAnalysisReportAsync(int departmentId, DateTime startDate, DateTime endDate)
        {
            // В этом методе будет реализована логика генерации отчета по критериям оценки
            throw new NotImplementedException();
        }

        /// <summary>
        /// Генерация отчета по динамике показателей эффективности
        /// </summary>
        public async Task<PerformanceTrendReport> GeneratePerformanceTrendReportAsync(int employeeId, DateTime startDate, DateTime endDate)
        {
            // В этом методе будет реализована логика генерации отчета по динамике показателей
            throw new NotImplementedException();
        }

        /// <summary>
        /// Генерация отчета по посещаемости
        /// </summary>
        public async Task<AttendanceReport> GenerateAttendanceReportAsync(int departmentId, DateTime startDate, DateTime endDate)
        {
            // В этом методе будет реализована логика генерации отчета по посещаемости
            throw new NotImplementedException();
        }

        /// <summary>
        /// Генерация аналитического отчета
        /// </summary>
        public async Task<AnalyticalReport> GenerateAnalyticalReportAsync(ReportParameters parameters)
        {
            // В этом методе будет реализована логика генерации аналитического отчета
            throw new NotImplementedException();
        }

        /// <summary>
        /// Получение списка сохраненных отчетов
        /// </summary>
        public async Task<List<ReportInfo>> GetSavedReportsAsync(int userId)
        {
            // В реальной реализации будет использоваться доступ к БД
            // Здесь упрощенная реализация для демонстрации структуры
            var reportFiles = Directory.GetFiles(_reportsDirectory, $"*_user{userId}_*.json");
            var reports = new List<ReportInfo>();

            foreach (var file in reportFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var reportInfo = JsonSerializer.Deserialize<ReportInfo>(json);
                    if (reportInfo != null)
                    {
                        reports.Add(reportInfo);
                    }
                }
                catch
                {
                    // Логирование ошибки
                }
            }

            return reports.OrderByDescending(r => r.GeneratedAt).ToList();
        }

        /// <summary>
        /// Получение информации об отчете по ID
        /// </summary>
        public async Task<ReportInfo> GetReportInfoByIdAsync(int reportId)
        {
            var reportFiles = Directory.GetFiles(_reportsDirectory, $"{reportId}_*.json");
            if (reportFiles.Length == 0)
            {
                return null;
            }

            try
            {
                var json = await File.ReadAllTextAsync(reportFiles[0]);
                return JsonSerializer.Deserialize<ReportInfo>(json);
            }
            catch
            {
                // Логирование ошибки
                return null;
            }
        }

        /// <summary>
        /// Сохранение отчета
        /// </summary>
        public async Task<bool> SaveReportAsync(ReportInfo reportInfo, byte[] reportData)
        {
            try
            {
                // Сохранение информации об отчете
                var infoJson = JsonSerializer.Serialize(reportInfo);
                var infoFilePath = Path.Combine(_reportsDirectory, $"{reportInfo.Id}_user{reportInfo.OwnerId}_info.json");
                await File.WriteAllTextAsync(infoFilePath, infoJson);

                // Сохранение данных отчета
                var dataFilePath = Path.Combine(_reportsDirectory, $"{reportInfo.Id}_user{reportInfo.OwnerId}_data.{reportInfo.FileFormat.ToLower()}");
                await File.WriteAllBytesAsync(dataFilePath, reportData);

                // Обновление пути к файлу
                reportInfo.FilePath = dataFilePath;
                infoJson = JsonSerializer.Serialize(reportInfo);
                await File.WriteAllTextAsync(infoFilePath, infoJson);

                return true;
            }
            catch
            {
                // Логирование ошибки
                return false;
            }
        }

        /// <summary>
        /// Получение данных отчета
        /// </summary>
        public async Task<byte[]> GetReportDataAsync(int reportId)
        {
            var reportFiles = Directory.GetFiles(_reportsDirectory, $"{reportId}_*_data.*");
            if (reportFiles.Length == 0)
            {
                return null;
            }

            try
            {
                return await File.ReadAllBytesAsync(reportFiles[0]);
            }
            catch
            {
                // Логирование ошибки
                return null;
            }
        }

        /// <summary>
        /// Удаление отчета
        /// </summary>
        public async Task<bool> DeleteReportAsync(int reportId)
        {
            try
            {
                var infoFiles = Directory.GetFiles(_reportsDirectory, $"{reportId}_*_info.json");
                var dataFiles = Directory.GetFiles(_reportsDirectory, $"{reportId}_*_data.*");

                foreach (var file in infoFiles.Concat(dataFiles))
                {
                    File.Delete(file);
                }

                return true;
            }
            catch
            {
                // Логирование ошибки
                return false;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Расчет статистики посещаемости
        /// </summary>
        private AttendanceStatistics CalculateAttendanceStatistics(List<AttendanceRecord> attendanceRecords, List<LeaveRecord> leaveRecords, DateTime startDate, DateTime endDate)
        {
            var stats = new AttendanceStatistics();

            // Расчет общего количества рабочих дней в периоде
            // Для упрощения считаем все дни с понедельника по пятницу рабочими
            // В реальной реализации нужно учитывать праздники и выходные дни
            int totalDays = 0;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    totalDays++;
                }
            }

            stats.TotalWorkDays = totalDays;

            // Расчет дней присутствия
            stats.DaysPresent = attendanceRecords.Select(a => a.Date.Date).Distinct().Count();

            // Расчет дней отпуска
            stats.VacationDays = leaveRecords
                .Where(l => l.Type == 1) // Тип 1 - отпуск
                .Sum(l => (l.EndDate.Date - l.StartDate.Date).Days + 1);

            // Расчет дней болезни
            stats.SickDays = leaveRecords
                .Where(l => l.Type == 2) // Тип 2 - больничный
                .Sum(l => (l.EndDate.Date - l.StartDate.Date).Days + 1);

            // Расчет опозданий
            stats.LateArrivals = attendanceRecords.Count(a => a.CheckIn.TimeOfDay > new TimeSpan(9, 0, 0)); // Считаем опозданием приход после 9:00

            // Расчет дней отсутствия
            stats.DaysAbsent = stats.TotalWorkDays - stats.DaysPresent - stats.VacationDays - stats.SickDays;
            if (stats.DaysAbsent < 0) stats.DaysAbsent = 0;

            // Расчет коэффициента посещаемости
            if (stats.TotalWorkDays > 0)
            {
                stats.AttendanceRate = (double)(stats.DaysPresent + stats.VacationDays + stats.SickDays) / stats.TotalWorkDays;
            }

            return stats;
        }

        /// <summary>
        /// Расчет статистики задач
        /// </summary>
        private TaskStatistics CalculateTaskStatistics(List<TaskRecord> taskRecords, DateTime startDate, DateTime endDate)
        {
            var stats = new TaskStatistics();

            // Общее количество задач
            stats.TotalTasks = taskRecords.Count;

            // Количество выполненных задач
            stats.CompletedTasks = taskRecords.Count(t => t.CompletedAt >= startDate && t.CompletedAt <= endDate);

            // Средняя эффективность
            if (stats.CompletedTasks > 0)
            {
                stats.AverageEfficiency = taskRecords
                    .Where(t => t.CompletedAt >= startDate && t.CompletedAt <= endDate && t.EfficiencyScore.HasValue)
                    .Average(t => t.EfficiencyScore.Value);
            }

            // Расчет динамики эффективности по месяцам
            var tasksGroupedByMonth = taskRecords
                .Where(t => t.CompletedAt >= startDate && t.CompletedAt <= endDate && t.EfficiencyScore.HasValue)
                .GroupBy(t => new DateTime(t.CompletedAt.Year, t.CompletedAt.Month, 1))
                .OrderBy(g => g.Key);

            foreach (var group in tasksGroupedByMonth)
            {
                stats.EfficiencyTrend.Add(new TaskEfficiencyTrend
                {
                    Month = group.Key,
                    CompletedTasks = group.Count(),
                    AverageEfficiency = group.Average(t => t.EfficiencyScore.Value)
                });
            }

            return stats;
        }

        /// <summary>
        /// Расчет оценок по критериям
        /// </summary>
        private async Task<List<EvaluationCriterionScore>> CalculateCriteriaScoresAsync(int employeeId, List<EmployeeEvaluation> evaluations)
        {
            var result = new List<EvaluationCriterionScore>();

            // Получение всех критериев оценки
            var allCriteria = await _evaluationService.GetAllCriteriaAsync();

            // Получение всех оценок по критериям для данного сотрудника
            var criteriaScores = new List<EvaluationScore>();
            foreach (var evaluation in evaluations)
            {
                var scores = await _evaluationService.GetScoresByEvaluationIdAsync(evaluation.Id);
                criteriaScores.AddRange(scores);
            }

            // Расчет средних оценок по критериям
            foreach (var criterion in allCriteria)
            {
                var scores = criteriaScores.Where(s => s.CriterionId == criterion.Id).ToList();
                if (scores.Any())
                {
                    result.Add(new EvaluationCriterionScore
                    {
                        Criterion = criterion,
                        AverageScore = scores.Average(s => s.Score),
                        // Средние значения по компании и отделу будут рассчитаны в реальной реализации
                        CompanyAverage = 0,
                        DepartmentAverage = 0
                    });
                }
            }

            return result;
        }

        #endregion
    }
}
