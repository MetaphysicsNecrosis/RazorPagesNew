using RazorPagesNew.Models.Activity;
using RazorPagesNew.Models;

namespace RazorPagesNew.Services.Interfaces
{
    public interface IEmployeeService
    {
        // Основные CRUD операции
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task<Employee> GetEmployeeByIdAsync(int id);
        Task<Employee> CreateEmployeeAsync(Employee employee);
        Task<Employee> UpdateEmployeeAsync(Employee employee);
        Task<bool> DeleteEmployeeAsync(int id);

        // Расширенные операции для сотрудников
        Task<IEnumerable<Employee>> GetEmployeesByDepartmentAsync(int departmentId);
        Task<IEnumerable<Employee>> SearchEmployeesAsync(string searchTerm);

        // Операции с записями активности
        Task<IEnumerable<AttendanceRecord>> GetEmployeeAttendanceAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<LeaveRecord>> GetEmployeeLeaveRecordsAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<TaskRecord>> GetEmployeeTasksAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null);

        // Статистика сотрудника
        Task<double> GetEmployeeVacationBalanceAsync(int employeeId);
        Task<int> GetEmployeeCompletedTasksCountAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null);
        Task<double> GetEmployeeAverageEfficiencyAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null);

        // Операции по управлению учетными записями сотрудников
        Task<bool> LinkEmployeeToUserAsync(int employeeId, int userId);
        Task<bool> UnlinkEmployeeFromUserAsync(int employeeId);
    }
}
