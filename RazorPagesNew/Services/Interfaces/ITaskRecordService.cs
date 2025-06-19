using RazorPagesNew.ModelsDb;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RazorPagesNew.Services.Interfaces
{
    public interface ITaskRecordService
    {
        /// <summary>
        /// Получение всех задач
        /// </summary>
        Task<IEnumerable<TaskRecord>> GetAllTaskRecordsAsync();

        /// <summary>
        /// Получение задачи по ID
        /// </summary>
        Task<TaskRecord> GetTaskRecordByIdAsync(int id);

        /// <summary>
        /// Создание новой задачи
        /// </summary>
        Task<TaskRecord> CreateTaskRecordAsync(TaskRecord taskRecord);

        /// <summary>
        /// Обновление задачи
        /// </summary>
        Task<TaskRecord> UpdateTaskRecordAsync(TaskRecord taskRecord);

        /// <summary>
        /// Удаление задачи
        /// </summary>
        Task<bool> DeleteTaskRecordAsync(int id);

        /// <summary>
        /// Получение задач для конкретного сотрудника
        /// </summary>
        Task<IEnumerable<TaskRecord>> GetTaskRecordsByEmployeeIdAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Поиск задач по различным критериям
        /// </summary>
        Task<IEnumerable<TaskRecord>> SearchTaskRecordsAsync(
            string searchTerm = null,
            int? employeeId = null,
            int? departmentId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? minImportance = null);

        /// <summary>
        /// Подсчёт количества выполненных задач по сотруднику и периоду
        /// </summary>
        Task<int> CountTasksAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Получение средней эффективности выполнения задач по сотруднику и периоду
        /// </summary>
        Task<double> GetAverageEfficiencyAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Получение задач с группировкой по отделам
        /// </summary>
        Task<Dictionary<int, List<TaskRecord>>> GetTasksByDepartmentsAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Получение списка недавних задач
        /// </summary>
        Task<IEnumerable<TaskRecord>> GetRecentTasksAsync(int count = 10);
    }
}