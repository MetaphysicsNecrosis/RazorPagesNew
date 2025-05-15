using RazorPagesNew.ModelsDb;

namespace RazorPagesNew.Services.Interfaces
{
    public interface IEvaluationService
    {
        // Операции с оценками сотрудников
        Task<IEnumerable<EmployeeEvaluation>> GetAllEvaluationsAsync();
        Task<EmployeeEvaluation> GetEvaluationByIdAsync(int id);
        Task<IEnumerable<EmployeeEvaluation>> GetEvaluationsByEmployeeIdAsync(int employeeId);
        Task<EmployeeEvaluation> CreateEvaluationAsync(EmployeeEvaluation evaluation);
        Task<EmployeeEvaluation> UpdateEvaluationAsync(EmployeeEvaluation evaluation);
        Task<bool> DeleteEvaluationAsync(int id);

        // Операции с критериями оценки
        Task<IEnumerable<EvaluationCriterion>> GetAllCriteriaAsync();
        Task<EvaluationCriterion> GetCriterionByIdAsync(int id);
        Task<EvaluationCriterion> CreateCriterionAsync(EvaluationCriterion criterion);
        Task<EvaluationCriterion> UpdateCriterionAsync(EvaluationCriterion criterion);
        Task<bool> DeleteCriterionAsync(int id);

        // Операции с оценками по критериям
        Task<IEnumerable<EvaluationScore>> GetScoresByEvaluationIdAsync(int evaluationId);
        Task<EvaluationScore> GetScoreByIdAsync(int id);
        Task<EvaluationScore> CreateScoreAsync(EvaluationScore score);
        Task<EvaluationScore> UpdateScoreAsync(EvaluationScore score);
        Task<bool> DeleteScoreAsync(int id);

        // Операции со сводками активности
        Task<WorkActivitySummary> GetWorkActivitySummaryByIdAsync(int id);
        Task<IEnumerable<WorkActivitySummary>> GetWorkActivitySummariesByEmployeeIdAsync(int employeeId);
        Task<WorkActivitySummary> CreateWorkActivitySummaryAsync(WorkActivitySummary summary);
        Task<WorkActivitySummary> UpdateWorkActivitySummaryAsync(WorkActivitySummary summary);
        Task<bool> DeleteWorkActivitySummaryAsync(int id);

        // Расчеты и аналитика
        Task<WorkActivitySummary> GenerateWorkActivitySummaryAsync(int employeeId, DateTime startDate, DateTime endDate);
        Task<double> CalculateOverallScoreAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null);
    }
}
