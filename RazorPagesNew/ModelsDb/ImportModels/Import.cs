using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using RazorPagesNew.Models.Import;
using RazorPagesNew.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace RazorPagesNew.Models.Import
{
    /// <summary>
    /// DTO для импорта данных о сотруднике
    /// </summary>
    public class EmployeeImportDto
    {
        /// <summary>
        /// Номер строки в исходном файле
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// ФИО сотрудника
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Email сотрудника
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Телефон сотрудника
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Должность сотрудника
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// Название отдела
        /// </summary>
        public string Department { get; set; }

        /// <summary>
        /// Дата приема на работу
        /// </summary>
        public DateTime? HireDate { get; set; }
    }

    /// <summary>
    /// Результат импорта сотрудников
    /// </summary>
    public class ImportResult
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
        /// Количество добавленных сотрудников
        /// </summary>
        public int AddedCount { get; set; }

        /// <summary>
        /// Количество обновленных сотрудников
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// Список ошибок при импорте
        /// </summary>
        public List<ImportError> Errors { get; set; } = new List<ImportError>();
    }

    /// <summary>
    /// Ошибка при импорте
    /// </summary>
    public class ImportError
    {
        /// <summary>
        /// Номер строки, в которой произошла ошибка
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// Имя поля, в котором произошла ошибка
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Результат валидации данных
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Флаг валидности данных
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Список ошибок валидации
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
    }

    /// <summary>
    /// Ошибка валидации
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Номер строки, в которой произошла ошибка валидации
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// Имя поля, в котором произошла ошибка валидации
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// Сообщение об ошибке валидации
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}