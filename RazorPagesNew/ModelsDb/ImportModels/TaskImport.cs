using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Models.Import
{
    /// <summary>
    /// DTO для импорта данных о задачах
    /// </summary>
    public class TaskRecordImportDto
    {
        /// <summary>
        /// Номер строки в исходном файле
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// Email сотрудника, которому назначена задача
        /// </summary>
        public string EmployeeEmail { get; set; }

        /// <summary>
        /// ID сотрудника, которому назначена задача
        /// </summary>
        public int? EmployeeId { get; set; }

        /// <summary>
        /// Заголовок задачи
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Описание задачи
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Дата выполнения задачи
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Идентификатор задачи во внешней системе
        /// </summary>
        public string ExternalSystemId { get; set; }

        /// <summary>
        /// Оценка эффективности выполнения задачи (0-100)
        /// </summary>
        public double? EfficiencyScore { get; set; }

        /// <summary>
        /// Важность задачи (0 - низкая, 1 - средняя, 2 - высокая, 3 - критическая)
        /// </summary>
        public int Importance { get; set; } = 1;
    }

    /// <summary>
    /// DTO для экспорта данных о задачах
    /// </summary>
    public class TaskRecordExportDto
    {
        /// <summary>
        /// ID задачи
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ID сотрудника
        /// </summary>
        public int EmployeeId { get; set; }

        /// <summary>
        /// ФИО сотрудника
        /// </summary>
        public string EmployeeName { get; set; }

        /// <summary>
        /// Email сотрудника
        /// </summary>
        public string EmployeeEmail { get; set; }

        /// <summary>
        /// Отдел сотрудника
        /// </summary>
        public string DepartmentName { get; set; }

        /// <summary>
        /// Заголовок задачи
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Описание задачи
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Дата выполнения задачи
        /// </summary>
        public DateTime CompletedAt { get; set; }

        /// <summary>
        /// Идентификатор задачи во внешней системе
        /// </summary>
        public string ExternalSystemId { get; set; }

        /// <summary>
        /// Оценка эффективности выполнения задачи (0-100)
        /// </summary>
        public double? EfficiencyScore { get; set; }

        /// <summary>
        /// Важность задачи (0 - низкая, 1 - средняя, 2 - высокая, 3 - критическая)
        /// </summary>
        public int Importance { get; set; }

        /// <summary>
        /// Имя пользователя, создавшего запись
        /// </summary>
        public string OwnerUsername { get; set; }

        /// <summary>
        /// Дата создания записи
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Модель для массового импорта задач
    /// </summary>
    public class TaskImportModel
    {
        /// <summary>
        /// Целевой отдел для импорта (по умолчанию)
        /// </summary>
        [Required]
        public int DefaultDepartmentId { get; set; }

        /// <summary>
        /// Обновлять существующие задачи
        /// </summary>
        public bool UpdateExisting { get; set; }

        /// <summary>
        /// Пропустить заголовок при импорте
        /// </summary>
        public bool SkipHeader { get; set; } = true;
    }

    /// <summary>
    /// Результат импорта задач
    /// </summary>
    public class TaskImportResult
    {
        /// <summary>
        /// Имя загруженного файла
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Тип файла (.xlsx, .docx, .xml)
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// Флаг успешности импорта
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Сообщение о результате импорта
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Количество обработанных строк
        /// </summary>
        public int ProcessedRows { get; set; }

        /// <summary>
        /// Количество успешно импортированных строк
        /// </summary>
        public int SuccessfulRows { get; set; }

        /// <summary>
        /// Количество добавленных задач
        /// </summary>
        public int AddedCount { get; set; }

        /// <summary>
        /// Количество обновленных задач
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// Список ошибок при импорте
        /// </summary>
        public List<ImportError> Errors { get; set; } = new List<ImportError>();
    }
}