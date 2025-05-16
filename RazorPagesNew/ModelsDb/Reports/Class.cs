namespace RazorPagesNew.ModelsDb.Reports
{
    /// <summary>
    /// Базовый интерфейс для всех отчетов
    /// </summary>
    public interface IReport
    {
        int Id { get; set; }
        string Title { get; set; }
        DateTime GeneratedAt { get; set; }
        ReportType Type { get; set; }
        int OwnerId { get; set; }
    }

    /// <summary>
    /// Типы отчетов
    /// </summary>
    public enum ReportType
    {
        EmployeeReport,
        DepartmentReport,
        CriteriaAnalysis,
        PerformanceTrend,
        Attendance,
        Analytical
    }

    /// <summary>
    /// Типы импорта данных
    /// </summary>
    public enum ImportType
    {
        EmployeeData,
        AttendanceData,
        EvaluationData,
        TaskData,
        LeaveData
    }

    /// <summary>
    /// Информация о сохраненном отчете
    /// </summary>
    public class ReportInfo
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public ReportType Type { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string Parameters { get; set; } // JSON-сериализованные параметры отчета
        public int OwnerId { get; set; }
        public string FilePath { get; set; } // Путь к сохраненному файлу отчета (если есть)
        public string FileFormat { get; set; } // Формат файла (PDF, Excel, XML)
    }

    /// <summary>
    /// Параметры для генерации отчетов
    /// </summary>
    public class ReportParameters
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? DepartmentId { get; set; }
        public List<int> EmployeeIds { get; set; } = new List<int>();
        public List<int> CriteriaIds { get; set; } = new List<int>();
        public bool IncludeGraphs { get; set; } = true;
        public bool IncludeDetails { get; set; } = true;
    }

    /// <summary>
    /// Индивидуальный отчет по сотруднику
    /// </summary>
    public class EmployeeReport : IReport
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime GeneratedAt { get; set; }
        public ReportType Type { get; set; } = ReportType.EmployeeReport;
        public int OwnerId { get; set; }

        public ModelsDb.Employee Employee { get; set; }
        public List<ModelsDb.EmployeeEvaluation> Evaluations { get; set; } = new List<ModelsDb.EmployeeEvaluation>();
        public List<ModelsDb.WorkActivitySummary> ActivitySummaries { get; set; } = new List<ModelsDb.WorkActivitySummary>();
        public double AverageScore { get; set; }
        public List<EvaluationCriterionScore> CriteriaScores { get; set; } = new List<EvaluationCriterionScore>();
        public AttendanceStatistics AttendanceStats { get; set; }
        public TaskStatistics TaskStats { get; set; }
    }

    /// <summary>
    /// Отчет по отделу
    /// </summary>
    public class DepartmentReport : IReport
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime GeneratedAt { get; set; }
        public ReportType Type { get; set; } = ReportType.DepartmentReport;
        public int OwnerId { get; set; }

        public ModelsDb.Department Department { get; set; }
        public List<EmployeeRating> EmployeeRatings { get; set; } = new List<EmployeeRating>();
        public double DepartmentAverageScore { get; set; }
        public List<EvaluationCriterionScore> DepartmentCriteriaScores { get; set; } = new List<EvaluationCriterionScore>();
        public AttendanceStatistics DepartmentAttendanceStats { get; set; }
        public TaskStatistics DepartmentTaskStats { get; set; }
    }

    /// <summary>
    /// Отчет по критериям оценки
    /// </summary>
    public class CriteriaAnalysisReport : IReport
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime GeneratedAt { get; set; }
        public ReportType Type { get; set; } = ReportType.CriteriaAnalysis;
        public int OwnerId { get; set; }

        public List<CriterionAnalysis> CriteriaAnalyses { get; set; } = new List<CriterionAnalysis>();
        public int? DepartmentId { get; set; }
        public ModelsDb.Department Department { get; set; }
    }

    /// <summary>
    /// Отчет по динамике показателей эффективности
    /// </summary>
    public class PerformanceTrendReport : IReport
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime GeneratedAt { get; set; }
        public ReportType Type { get; set; } = ReportType.PerformanceTrend;
        public int OwnerId { get; set; }

        public ModelsDb.Employee Employee { get; set; }
        public List<PerformancePoint> EvaluationTrend { get; set; } = new List<PerformancePoint>();
        public List<PerformancePoint> TaskEfficiencyTrend { get; set; } = new List<PerformancePoint>();
        public List<PerformancePoint> AttendanceTrend { get; set; } = new List<PerformancePoint>();
    }

    /// <summary>
    /// Отчет по посещаемости
    /// </summary>
    public class AttendanceReport : IReport
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime GeneratedAt { get; set; }
        public ReportType Type { get; set; } = ReportType.Attendance;
        public int OwnerId { get; set; }

        public ModelsDb.Department Department { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<EmployeeAttendance> EmployeeAttendances { get; set; } = new List<EmployeeAttendance>();
        public AttendanceStatistics DepartmentAttendanceStats { get; set; }
    }

    /// <summary>
    /// Аналитический отчет с графиками и диаграммами
    /// </summary>
    public class AnalyticalReport : IReport
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime GeneratedAt { get; set; }
        public ReportType Type { get; set; } = ReportType.Analytical;
        public int OwnerId { get; set; }

        public ReportParameters Parameters { get; set; }
        public List<DepartmentPerformance> DepartmentPerformances { get; set; } = new List<DepartmentPerformance>();
        public List<EmployeeRating> TopPerformers { get; set; } = new List<EmployeeRating>();
        public List<EmployeeRating> LowPerformers { get; set; } = new List<EmployeeRating>();
        public List<CriterionAnalysis> CriteriaAnalyses { get; set; } = new List<CriterionAnalysis>();
        public List<AttendanceMonthlyStats> AttendanceTrend { get; set; } = new List<AttendanceMonthlyStats>();
    }

    // Вспомогательные классы для отчетов

    /// <summary>
    /// Оценки по критерию для сотрудника или отдела
    /// </summary>
    public class EvaluationCriterionScore
    {
        public ModelsDb.EvaluationCriterion Criterion { get; set; }
        public double AverageScore { get; set; }
        public double CompanyAverage { get; set; }
        public double DepartmentAverage { get; set; }
    }

    /// <summary>
    /// Статистика посещаемости
    /// </summary>
    public class AttendanceStatistics
    {
        public int TotalWorkDays { get; set; }
        public int DaysPresent { get; set; }
        public int DaysAbsent { get; set; }
        public int LateArrivals { get; set; }
        public int VacationDays { get; set; }
        public int SickDays { get; set; }
        public double AttendanceRate { get; set; }
    }

    /// <summary>
    /// Статистика задач
    /// </summary>
    public class TaskStatistics
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public double AverageEfficiency { get; set; }
        public List<TaskEfficiencyTrend> EfficiencyTrend { get; set; } = new List<TaskEfficiencyTrend>();
    }

    /// <summary>
    /// Динамика эффективности выполнения задач
    /// </summary>
    public class TaskEfficiencyTrend
    {
        public DateTime Month { get; set; }
        public int CompletedTasks { get; set; }
        public double AverageEfficiency { get; set; }
    }

    /// <summary>
    /// Рейтинг сотрудника
    /// </summary>
    public class EmployeeRating
    {
        public ModelsDb.Employee Employee { get; set; }
        public double AverageScore { get; set; }
        public int CompletedTasks { get; set; }
        public double AttendanceRate { get; set; }
        public int Rank { get; set; }
    }

    /// <summary>
    /// Анализ критерия оценки
    /// </summary>
    public class CriterionAnalysis
    {
        public ModelsDb.EvaluationCriterion Criterion { get; set; }
        public double AverageScore { get; set; }
        public double MinScore { get; set; }
        public double MaxScore { get; set; }
        public double MedianScore { get; set; }
        public List<DepartmentCriterionScore> DepartmentScores { get; set; } = new List<DepartmentCriterionScore>();
    }

    /// <summary>
    /// Оценка отдела по критерию
    /// </summary>
    public class DepartmentCriterionScore
    {
        public ModelsDb.Department Department { get; set; }
        public double AverageScore { get; set; }
    }

    /// <summary>
    /// Точка для графика динамики показателей
    /// </summary>
    public class PerformancePoint
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
    }

    /// <summary>
    /// Данные о посещаемости сотрудника
    /// </summary>
    public class EmployeeAttendance
    {
        public ModelsDb.Employee Employee { get; set; }
        public List<ModelsDb.AttendanceRecord> AttendanceRecords { get; set; } = new List<ModelsDb.AttendanceRecord>();
        public List<ModelsDb.LeaveRecord> LeaveRecords { get; set; } = new List<ModelsDb.LeaveRecord>();
        public AttendanceStatistics Statistics { get; set; }
    }

    /// <summary>
    /// Производительность отдела
    /// </summary>
    public class DepartmentPerformance
    {
        public ModelsDb.Department Department { get; set; }
        public double AverageScore { get; set; }
        public double AttendanceRate { get; set; }
        public int CompletedTasks { get; set; }
        public int EmployeeCount { get; set; }
    }

    /// <summary>
    /// Ежемесячная статистика посещаемости
    /// </summary>
    public class AttendanceMonthlyStats
    {
        public DateTime Month { get; set; }
        public double AttendanceRate { get; set; }
        public int VacationDays { get; set; }
        public int SickDays { get; set; }
    }

    // Классы для импорта данных

    /// <summary>
    /// Данные для импорта
    /// </summary>
    public class ImportData
    {
        public ImportType Type { get; set; }
        public List<Dictionary<string, object>> Rows { get; set; } = new List<Dictionary<string, object>>();
    }

    /// <summary>
    /// Результат импорта данных
    /// </summary>
    public class ImportResult
    {
        public bool Success { get; set; }
        public int ProcessedRows { get; set; }
        public int SuccessfulRows { get; set; }
        public List<ImportError> Errors { get; set; } = new List<ImportError>();
        public string Message { get; set; }
    }

    /// <summary>
    /// Ошибка импорта
    /// </summary>
    public class ImportError
    {
        public int RowNumber { get; set; }
        public string FieldName { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Результат валидации данных
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
    }

    /// <summary>
    /// Ошибка валидации
    /// </summary>
    public class ValidationError
    {
        public int RowNumber { get; set; }
        public string FieldName { get; set; }
        public string ErrorMessage { get; set; }
    }
}
