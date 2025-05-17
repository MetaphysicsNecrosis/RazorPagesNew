using RazorPagesNew.Models.Import;

namespace RazorPagesNew.Services.Interfaces
{
    /// <summary>
    /// Сервис для импорта сотрудников
    /// </summary>
    public interface IEmployeeImportService
    {
        /// <summary>
        /// Импортирует сотрудников из списка DTO
        /// </summary>
        Task<ImportResult> ImportEmployeesAsync(
            List<EmployeeImportDto> employees,
            int defaultDepartmentId,
            bool updateExisting,
            string username);
    }
}