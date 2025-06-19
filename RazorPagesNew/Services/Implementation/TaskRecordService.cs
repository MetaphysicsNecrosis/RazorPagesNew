using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesNew.Services.Implementation
{
    public class TaskRecordService : ITaskRecordService
    {
        private readonly MyApplicationDbContext _context;

        public TaskRecordService(MyApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получение всех задач
        /// </summary>
        public async Task<IEnumerable<TaskRecord>> GetAllTaskRecordsAsync()
        {
            return await _context.TaskRecords
                .Include(t => t.Employee)
                .Include(t => t.Owner)
                .OrderByDescending(t => t.CompletedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Получение задачи по ID
        /// </summary>
        public async Task<TaskRecord> GetTaskRecordByIdAsync(int id)
        {
            return await _context.TaskRecords
                .Include(t => t.Employee)
                .Include(t => t.Owner)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        /// <summary>
        /// Создание новой задачи
        /// </summary>
        public async Task<TaskRecord> CreateTaskRecordAsync(TaskRecord taskRecord)
        {
            if (taskRecord == null)
                throw new ArgumentNullException(nameof(taskRecord));

            await _context.TaskRecords.AddAsync(taskRecord);
            await _context.SaveChangesAsync();

            return taskRecord;
        }

        /// <summary>
        /// Обновление задачи
        /// </summary>
        public async Task<TaskRecord> UpdateTaskRecordAsync(TaskRecord taskRecord)
        {
            if (taskRecord == null)
                throw new ArgumentNullException(nameof(taskRecord));

            var existingTask = await _context.TaskRecords.FindAsync(taskRecord.Id);
            if (existingTask == null)
                throw new KeyNotFoundException($"Задача с ID {taskRecord.Id} не найдена.");

            _context.Entry(existingTask).CurrentValues.SetValues(taskRecord);
            taskRecord.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return existingTask;
        }

        /// <summary>
        /// Удаление задачи
        /// </summary>
        public async Task<bool> DeleteTaskRecordAsync(int id)
        {
            var taskRecord = await _context.TaskRecords.FindAsync(id);
            if (taskRecord == null)
                return false;

            _context.TaskRecords.Remove(taskRecord);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Получение задач для конкретного сотрудника
        /// </summary>
        public async Task<IEnumerable<TaskRecord>> GetTaskRecordsByEmployeeIdAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.TaskRecords
                .Where(t => t.EmployeeId == employeeId);

            // Применяем фильтр по дате, если указан
            if (startDate.HasValue)
                query = query.Where(t => t.CompletedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.CompletedAt <= endDate.Value);

            return await query
                .Include(t => t.Employee)
                .Include(t => t.Owner)
                .OrderByDescending(t => t.CompletedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Поиск задач по различным критериям
        /// </summary>
        public async Task<IEnumerable<TaskRecord>> SearchTaskRecordsAsync(
            string searchTerm = null,
            int? employeeId = null,
            int? departmentId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? minImportance = null)
        {
            var query = _context.TaskRecords
                .Include(t => t.Employee)
                .ThenInclude(e => e.Department)
                .Include(t => t.Owner)
                .AsQueryable();

            // Фильтрация по поисковому запросу
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(searchTerm) ||
                    (t.Description != null && t.Description.ToLower().Contains(searchTerm)) ||
                    t.Employee.FullName.ToLower().Contains(searchTerm) ||
                    (t.ExternalSystemId != null && t.ExternalSystemId.ToLower().Contains(searchTerm))
                );
            }

            // Фильтрация по сотруднику
            if (employeeId.HasValue)
            {
                query = query.Where(t => t.EmployeeId == employeeId.Value);
            }

            // Фильтрация по отделу
            if (departmentId.HasValue)
            {
                query = query.Where(t => t.Employee.DepartmentId == departmentId.Value);
            }

            // Фильтрация по периоду
            if (startDate.HasValue)
            {
                query = query.Where(t => t.CompletedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(t => t.CompletedAt <= endDate.Value);
            }

            // Фильтрация по важности
            if (minImportance.HasValue)
            {
                query = query.Where(t => t.Importance >= minImportance.Value);
            }

            // Сортировка и возврат результатов
            return await query
                .OrderByDescending(t => t.CompletedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Подсчёт количества выполненных задач по сотруднику и периоду
        /// </summary>
        public async Task<int> CountTasksAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.TaskRecords
                .Where(t => t.EmployeeId == employeeId);

            if (startDate.HasValue)
                query = query.Where(t => t.CompletedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.CompletedAt <= endDate.Value);

            return await query.CountAsync();
        }

        /// <summary>
        /// Получение средней эффективности выполнения задач по сотруднику и периоду
        /// </summary>
        public async Task<double> GetAverageEfficiencyAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
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

        /// <summary>
        /// Получение задач с группировкой по отделам
        /// </summary>
        public async Task<Dictionary<int, List<TaskRecord>>> GetTasksByDepartmentsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.TaskRecords
                .Include(t => t.Employee)
                .ThenInclude(e => e.Department)
                .Include(t => t.Owner)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(t => t.CompletedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.CompletedAt <= endDate.Value);

            var tasks = await query
                .OrderByDescending(t => t.CompletedAt)
                .AsNoTracking()
                .ToListAsync();

            // Группировка по отделу
            return tasks
                .GroupBy(t => t.Employee.DepartmentId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// Получение списка недавних задач
        /// </summary>
        public async Task<IEnumerable<TaskRecord>> GetRecentTasksAsync(int count = 10)
        {
            return await _context.TaskRecords
                .Include(t => t.Employee)
                .Include(t => t.Owner)
                .OrderByDescending(t => t.CompletedAt)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}