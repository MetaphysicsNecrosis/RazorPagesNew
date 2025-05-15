using RazorPagesNew.Data;

using RazorPagesNew.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;

namespace RazorPagesNew.Services.Implementation
{
    public class EmployeeService : IEmployeeService
    {
        private readonly MyApplicationDbContext _context;

        public EmployeeService(MyApplicationDbContext context)
        {
            _context = context;
        }

        #region CRUD операции

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            return await _context.Employees
                .Include(e => e.Department)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Employee> GetEmployeeByIdAsync(int id)
        {
            return await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Users)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Employee> CreateEmployeeAsync(Employee employee)
        {
            if (employee == null)
                throw new ArgumentNullException(nameof(employee));

            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();

            return employee;
        }

        public async Task<Employee> UpdateEmployeeAsync(Employee employee)
        {
            if (employee == null)
                throw new ArgumentNullException(nameof(employee));

            var existingEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employee.Id);

            if (existingEmployee == null)
                throw new KeyNotFoundException($"Employee with ID {employee.Id} not found.");

            _context.Entry(existingEmployee).CurrentValues.SetValues(employee);
            await _context.SaveChangesAsync();

            return existingEmployee;
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return false;

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Расширенные операции

        public async Task<IEnumerable<Employee>> GetEmployeesByDepartmentAsync(int departmentId)
        {
            return await _context.Employees
                .Where(e => e.DepartmentId == departmentId)
                .Include(e => e.Department)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Employee>> SearchEmployeesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllEmployeesAsync();

            return await _context.Employees
                .Where(e => e.FullName.Contains(searchTerm) ||
                       e.Email.Contains(searchTerm) ||
                       e.Position.Contains(searchTerm) ||
                       e.Department.Name.Contains(searchTerm))
                .Include(e => e.Department)
                .AsNoTracking()
                .ToListAsync();
        }

        #endregion

        #region Операции с записями активности

        public async Task<IEnumerable<AttendanceRecord>> GetEmployeeAttendanceAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.AttendanceRecords
                .Where(a => a.EmployeeId == employeeId);

            if (startDate.HasValue)
                query = query.Where(a => a.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Date <= endDate.Value);

            return await query.OrderByDescending(a => a.Date).ToListAsync();
        }

        public async Task<IEnumerable<LeaveRecord>> GetEmployeeLeaveRecordsAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.LeaveRecords
                .Where(l => l.EmployeeId == employeeId);

            if (startDate.HasValue)
                query = query.Where(l => l.StartDate >= startDate.Value || l.EndDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.StartDate <= endDate.Value || l.EndDate <= endDate.Value);

            return await query.OrderByDescending(l => l.StartDate).ToListAsync();
        }

        public async Task<IEnumerable<TaskRecord>> GetEmployeeTasksAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.TaskRecords
                .Where(t => t.EmployeeId == employeeId);

            if (startDate.HasValue)
                query = query.Where(t => t.CompletedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.CompletedAt <= endDate.Value);

            return await query.OrderByDescending(t => t.CompletedAt).ToListAsync();
        }

        #endregion

        #region Статистика сотрудника

        public async Task<double> GetEmployeeVacationBalanceAsync(int employeeId)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            return employee?.VacationBalance ?? 0;
        }

        public async Task<int> GetEmployeeCompletedTasksCountAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.TaskRecords
                .Where(t => t.EmployeeId == employeeId);

            if (startDate.HasValue)
                query = query.Where(t => t.CompletedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.CompletedAt <= endDate.Value);

            return await query.CountAsync();
        }

        public async Task<double> GetEmployeeAverageEfficiencyAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.TaskRecords
                .Where(t => t.EmployeeId == employeeId && t.EfficiencyScore.HasValue);

            if (startDate.HasValue)
                query = query.Where(t => t.CompletedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.CompletedAt <= endDate.Value);

            var tasks = await query.ToListAsync();

            if (!tasks.Any())
                return 0;

            return tasks.Average(t => t.EfficiencyScore.Value);
        }

        #endregion

        #region Управление учетными записями

        public async Task<bool> LinkEmployeeToUserAsync(int employeeId, int userId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // Проверяем, что пользователь еще не связан с другим сотрудником
            if (user.EmployeeId.HasValue && user.EmployeeId.Value != employeeId)
                return false;

            user.EmployeeId = employeeId;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UnlinkEmployeeFromUserAsync(int employeeId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);

            if (user == null)
                return false;

            user.EmployeeId = null;
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}
