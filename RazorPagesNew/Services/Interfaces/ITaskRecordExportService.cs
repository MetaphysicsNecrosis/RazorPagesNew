using RazorPagesNew.Models.Import;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RazorPagesNew.Services.Interfaces
{
    public interface ITaskRecordExportService
    {
        /// <summary>
        /// Экспортирует задачи в формат Excel (XLSX)
        /// </summary>
        Task<byte[]> ExportToExcelAsync(
            IEnumerable<TaskRecordExportDto> tasks,
            string sheetName = "Задачи");

        /// <summary>
        /// Экспортирует задачи в формат CSV
        /// </summary>
        Task<byte[]> ExportToCsvAsync(
            IEnumerable<TaskRecordExportDto> tasks,
            string delimiter = ",",
            bool includeHeader = true);

        /// <summary>
        /// Конвертирует список задач в DTO для экспорта
        /// </summary>
        Task<IEnumerable<TaskRecordExportDto>> ConvertToExportDtoAsync(
            IEnumerable<ModelsDb.TaskRecord> tasks);

        /// <summary>
        /// Экспортирует задачи в PDF формат
        /// </summary>
        Task<byte[]> ExportToPdfAsync(
            IEnumerable<TaskRecordExportDto> tasks,
            string title = "Отчет по задачам");
    }
}