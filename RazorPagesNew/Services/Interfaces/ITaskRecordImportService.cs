
using RazorPagesNew.Models.Import;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RazorPagesNew.Services.Interfaces
{
    public interface ITaskRecordImportService
    {
        /// <summary>
        /// Импортирует задачи из списка DTO
        /// </summary>
        Task<TaskImportResult> ImportTasksAsync(
            List<TaskRecordImportDto> tasks,
            int defaultDepartmentId,
            bool updateExisting,
            string username);

        /// <summary>
        /// Парсит файл импорта и возвращает список DTO задач
        /// </summary>
        Task<List<TaskRecordImportDto>> ParseImportFileAsync(byte[] fileContent, string fileName, bool skipHeader);
    }
}