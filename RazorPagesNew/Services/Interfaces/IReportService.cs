using RazorPagesNew.ModelsDb.Reports;
using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Services.Interfaces
{
    public interface IReportService
    {
        Task<EmployeeReport> GenerateEmployeeReportAsync(int employeeId, DateTime startDate, DateTime endDate);
        Task<DepartmentReport> GenerateDepartmentReportAsync(int departmentId, DateTime startDate, DateTime endDate);
        Task<CriteriaAnalysisReport> GenerateCriteriaAnalysisReportAsync(int departmentId, DateTime startDate, DateTime endDate);
        Task<PerformanceTrendReport> GeneratePerformanceTrendReportAsync(int employeeId, DateTime startDate, DateTime endDate);
        Task<AttendanceReport> GenerateAttendanceReportAsync(int departmentId, DateTime startDate, DateTime endDate);
        Task<AnalyticalReport> GenerateAnalyticalReportAsync(ReportParameters parameters);

        // Получение сохраненных отчетов
        Task<List<ReportInfo>> GetSavedReportsAsync(int userId);
        Task<ReportInfo> GetReportInfoByIdAsync(int reportId);
        Task<bool> SaveReportAsync(ReportInfo reportInfo, byte[] reportData);
        Task<byte[]> GetReportDataAsync(int reportId);
        Task<bool> DeleteReportAsync(int reportId);
    }
}
