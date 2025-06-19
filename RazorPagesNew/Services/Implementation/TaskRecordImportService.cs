using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.IO;
using System.Text.RegularExpressions;
using RazorPagesNew.Models.Enums;
using RazorPagesNew.Models.Import;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Spreadsheet;

namespace RazorPagesNew.Services.Implementation
{
    public class TaskRecordImportService : ITaskRecordImportService
    {
        private readonly MyApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public TaskRecordImportService(
            MyApplicationDbContext context,
            IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Импортирует задачи из списка DTO
        /// </summary>
        public async Task<TaskImportResult> ImportTasksAsync(
            List<TaskRecordImportDto> tasks,
            int defaultDepartmentId,
            bool updateExisting,
            string username)
        {
            var result = new TaskImportResult
            {
                Success = true,
                ProcessedRows = 0,
                SuccessfulRows = 0,
                AddedCount = 0,
                UpdatedCount = 0,
                Message = "Импорт успешно завершен"
            };

            if (tasks == null || !tasks.Any())
            {
                result.Success = false;
                result.Message = "Не найдены данные для импорта";
                return result;
            }

            // Получаем Id текущего пользователя
            var ownerId = 1; // По умолчанию, если не найден
            if (!string.IsNullOrEmpty(username))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null)
                {
                    ownerId = user.Id;
                }
            }

            // Загружаем данные о сотрудниках для поиска по email
            var employeesByEmail = await _context.Employees
                .Where(e => tasks.Any(t => t.EmployeeEmail != null && t.EmployeeEmail.ToLower() == e.Email.ToLower()))
                .ToDictionaryAsync(e => e.Email.ToLower(), e => e);

            // Загружаем существующие задачи по ExternalSystemId для обновления
            var existingTasksByExternalId = new Dictionary<string, TaskRecord>();
            if (updateExisting)
            {
                var externalIds = tasks
                    .Where(t => !string.IsNullOrEmpty(t.ExternalSystemId))
                    .Select(t => t.ExternalSystemId)
                    .Distinct()
                    .ToList();

                if (externalIds.Any())
                {
                    var existingTasks = await _context.TaskRecords
                        .Where(t => t.ExternalSystemId != null && externalIds.Contains(t.ExternalSystemId))
                        .ToListAsync();

                    foreach (var task in existingTasks)
                    {
                        if (!string.IsNullOrEmpty(task.ExternalSystemId))
                        {
                            existingTasksByExternalId[task.ExternalSystemId] = task;
                        }
                    }
                }
            }

            // Массово обрабатываем задачи
            foreach (var dto in tasks)
            {
                result.ProcessedRows++;

                try
                {
                    // Валидация данных
                    var validationResult = ValidateTaskDto(dto);
                    if (!validationResult.IsValid)
                    {
                        foreach (var error in validationResult.Errors)
                        {
                            result.Errors.Add(new ImportError
                            {
                                RowNumber = dto.RowNumber,
                                FieldName = error.FieldName,
                                ErrorMessage = error.ErrorMessage
                            });
                        }
                        continue;
                    }

                    // Поиск сотрудника по email или ID
                    Employee employee = null;

                    if (dto.EmployeeId.HasValue)
                    {
                        employee = await _context.Employees.FindAsync(dto.EmployeeId.Value);
                    }
                    else if (!string.IsNullOrEmpty(dto.EmployeeEmail))
                    {
                        var normalizedEmail = dto.EmployeeEmail.ToLower();
                        if (employeesByEmail.ContainsKey(normalizedEmail))
                        {
                            employee = employeesByEmail[normalizedEmail];
                        }
                    }

                    if (employee == null)
                    {
                        result.Errors.Add(new ImportError
                        {
                            RowNumber = dto.RowNumber,
                            FieldName = "Сотрудник",
                            ErrorMessage = "Сотрудник не найден по ID или Email"
                        });
                        continue;
                    }

                    // Проверяем существование задачи по ExternalSystemId
                    if (!string.IsNullOrEmpty(dto.ExternalSystemId) &&
                        existingTasksByExternalId.TryGetValue(dto.ExternalSystemId, out var existingTask))
                    {
                        // Обновляем существующую задачу
                        if (updateExisting)
                        {
                            existingTask.Title = dto.Title;
                            existingTask.Description = dto.Description;
                            existingTask.EmployeeId = employee.Id;

                            if (dto.CompletedAt.HasValue)
                            {
                                existingTask.CompletedAt = dto.CompletedAt.Value;
                            }

                            if (dto.EfficiencyScore.HasValue)
                            {
                                existingTask.EfficiencyScore = dto.EfficiencyScore;
                            }

                            existingTask.Importance = dto.Importance;
                            existingTask.UpdatedAt = DateTime.UtcNow;

                            _context.TaskRecords.Update(existingTask);
                            result.UpdatedCount++;
                            result.SuccessfulRows++;
                        }
                        else
                        {
                            // Пропускаем существующую задачу, если обновление не разрешено
                            result.Errors.Add(new ImportError
                            {
                                RowNumber = dto.RowNumber,
                                FieldName = "ExternalSystemId",
                                ErrorMessage = "Задача с таким ExternalSystemId уже существует. Включите опцию 'Обновлять существующие задачи' для обновления данных."
                            });
                            continue;
                        }
                    }
                    else
                    {
                        // Создаем новую задачу
                        var newTask = new TaskRecord
                        {
                            EmployeeId = employee.Id,
                            Title = dto.Title,
                            Description = dto.Description,
                            CompletedAt = dto.CompletedAt ?? DateTime.UtcNow,
                            ExternalSystemId = dto.ExternalSystemId,
                            EfficiencyScore = dto.EfficiencyScore,
                            Importance = dto.Importance,
                            OwnerId = ownerId,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _context.TaskRecords.AddAsync(newTask);
                        result.AddedCount++;
                        result.SuccessfulRows++;
                    }
                }
                catch (Exception ex)
                {
                    // Записываем ошибку в результат
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = dto.RowNumber,
                        FieldName = "Обработка",
                        ErrorMessage = ex.Message
                    });
                }
            }

            // Сохраняем изменения в базе данных
            await _context.SaveChangesAsync();

            // Формируем итоговое сообщение
            if (result.ProcessedRows == 0)
            {
                result.Success = false;
                result.Message = "В файле не найдены данные о задачах.";
            }
            else if (result.SuccessfulRows == 0)
            {
                result.Success = false;
                result.Message = $"Не удалось импортировать ни одной задачи из {result.ProcessedRows} записей.";
            }
            else if (result.SuccessfulRows < result.ProcessedRows)
            {
                result.Success = true;
                result.Message = $"Частично успешный импорт. Обработано {result.ProcessedRows} записей, успешно импортировано {result.SuccessfulRows}.";
            }
            else
            {
                result.Success = true;
                result.Message = $"Импорт успешно завершен. Обработано {result.ProcessedRows} записей.";
            }

            // Логируем результат импорта
            await _auditLogService.LogActivityAsync(
                username ?? "system",
                ActionType.Import,
                "TaskRecord",
                "0",
                $"Импортировано задач: {result.SuccessfulRows}/{result.ProcessedRows}. Добавлено: {result.AddedCount}, обновлено: {result.UpdatedCount}"
            );

            return result;
        }

        /// <summary>
        /// Парсит файл импорта и возвращает список DTO задач
        /// </summary>
        public async Task<List<TaskRecordImportDto>> ParseImportFileAsync(byte[] fileContent, string fileName, bool skipHeader)
        {
            var result = new List<TaskRecordImportDto>();
            var fileExtension = Path.GetExtension(fileName).ToLower();

            if (fileExtension == ".xlsx" || fileExtension == ".xls")
            {
                result = ParseExcelFile(fileContent, fileExtension, skipHeader);
            }
            else if (fileExtension == ".csv")
            {
                result = ParseCsvFile(fileContent, skipHeader);
            }
            else
            {
                throw new NotSupportedException($"Формат файла {fileExtension} не поддерживается для импорта.");
            }

            return result;
        }

        /// <summary>
        /// Парсинг файла Excel
        /// </summary>
        private List<TaskRecordImportDto> ParseExcelFile(byte[] fileContent, string fileExtension, bool skipHeader)
        {
            var result = new List<TaskRecordImportDto>();

            using (var stream = new MemoryStream(fileContent))
            {
                IWorkbook workbook;

                if (fileExtension == ".xlsx")
                {
                    workbook = new XSSFWorkbook(stream);
                }
                else
                {
                    workbook = new HSSFWorkbook(stream);
                }

                // Берем первый лист
                var sheet = workbook.GetSheetAt(0);

                // Определяем индексы колонок по заголовкам
                var headerRow = sheet.GetRow(0);
                var columnIndexes = GetColumnIndexes(headerRow);

                // Начинаем с первой строки или со второй, если пропускаем заголовок
                int startRow = skipHeader ? 1 : 0;

                for (int i = startRow; i <= sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    if (row == null) continue;

                    try
                    {
                        var dto = CreateTaskDtoFromRow(row, columnIndexes, i + 1);
                        if (dto != null)
                        {
                            result.Add(dto);
                        }
                    }
                    catch (Exception)
                    {
                        // Пропускаем строку с ошибкой
                        continue;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Парсинг CSV файла
        /// </summary>
        private List<TaskRecordImportDto> ParseCsvFile(byte[] fileContent, bool skipHeader)
        {
            var result = new List<TaskRecordImportDto>();

            using (var stream = new MemoryStream(fileContent))
            using (var reader = new StreamReader(stream))
            {
                // Читаем заголовок для определения колонок
                string headerLine = reader.ReadLine();
                var headers = headerLine.Split(',', ';');
                var columnIndexes = GetColumnIndexesFromCsv(headers);

                // Пропускаем заголовок, если нужно
                int rowNumber = 1;

                // Если не пропускаем заголовок, то парсим первую строку как данные
                if (!skipHeader)
                {
                    reader.BaseStream.Position = 0;
                    reader.DiscardBufferedData();
                }

                // Читаем остальные строки
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    rowNumber++;

                    // Пропускаем заголовок
                    if (skipHeader && rowNumber == 2)
                    {
                        continue;
                    }

                    try
                    {
                        var values = line.Split(',', ';');
                        var dto = CreateTaskDtoFromCsv(values, columnIndexes, rowNumber);
                        if (dto != null)
                        {
                            result.Add(dto);
                        }
                    }
                    catch (Exception)
                    {
                        // Пропускаем строку с ошибкой
                        continue;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Получение индексов колонок из заголовков Excel
        /// </summary>
        private Dictionary<string, int> GetColumnIndexes(IRow headerRow)
        {
            var columnIndexes = new Dictionary<string, int>();

            for (int i = 0; i < headerRow.LastCellNum; i++)
            {
                var cell = headerRow.GetCell(i);
                if (cell != null)
                {
                    string headerName = cell.StringCellValue.Trim().ToLower();
                    switch (headerName)
                    {
                        case "employeeemail":
                        case "email сотрудника":
                        case "email":
                            columnIndexes["EmployeeEmail"] = i;
                            break;
                        case "employeeid":
                        case "id сотрудника":
                        case "сотрудник":
                            columnIndexes["EmployeeId"] = i;
                            break;
                        case "title":
                        case "заголовок":
                        case "название":
                            columnIndexes["Title"] = i;
                            break;
                        case "description":
                        case "описание":
                            columnIndexes["Description"] = i;
                            break;
                        case "completedat":
                        case "дата выполнения":
                        case "выполнено":
                            columnIndexes["CompletedAt"] = i;
                            break;
                        case "externalsystemid":
                        case "внешний id":
                        case "id во внешней системе":
                            columnIndexes["ExternalSystemId"] = i;
                            break;
                        case "efficiencyscore":
                        case "эффективность":
                        case "оценка эффективности":
                            columnIndexes["EfficiencyScore"] = i;
                            break;
                        case "importance":
                        case "важность":
                            columnIndexes["Importance"] = i;
                            break;
                    }
                }
            }

            return columnIndexes;
        }

        /// <summary>
        /// Получение индексов колонок из заголовков CSV
        /// </summary>
        private Dictionary<string, int> GetColumnIndexesFromCsv(string[] headers)
        {
            var columnIndexes = new Dictionary<string, int>();

            for (int i = 0; i < headers.Length; i++)
            {
                string headerName = headers[i].Trim().ToLower();
                switch (headerName)
                {
                    case "employeeemail":
                    case "email сотрудника":
                    case "email":
                        columnIndexes["EmployeeEmail"] = i;
                        break;
                    case "employeeid":
                    case "id сотрудника":
                    case "сотрудник":
                        columnIndexes["EmployeeId"] = i;
                        break;
                    case "title":
                    case "заголовок":
                    case "название":
                        columnIndexes["Title"] = i;
                        break;
                    case "description":
                    case "описание":
                        columnIndexes["Description"] = i;
                        break;
                    case "completedat":
                    case "дата выполнения":
                    case "выполнено":
                        columnIndexes["CompletedAt"] = i;
                        break;
                    case "externalsystemid":
                    case "внешний id":
                    case "id во внешней системе":
                        columnIndexes["ExternalSystemId"] = i;
                        break;
                    case "efficiencyscore":
                    case "эффективность":
                    case "оценка эффективности":
                        columnIndexes["EfficiencyScore"] = i;
                        break;
                    case "importance":
                    case "важность":
                        columnIndexes["Importance"] = i;
                        break;
                }
            }

            return columnIndexes;
        }

        /// <summary>
        /// Создание DTO задачи из строки Excel
        /// </summary>
        private TaskRecordImportDto CreateTaskDtoFromRow(IRow row, Dictionary<string, int> columnIndexes, int rowNumber)
        {
            if (!columnIndexes.ContainsKey("Title"))
            {
                // Если нет обязательного поля "Title", пропускаем строку
                return null;
            }

            var dto = new TaskRecordImportDto { RowNumber = rowNumber };

            // Заполняем поля DTO из ячеек
            if (columnIndexes.ContainsKey("EmployeeEmail"))
            {
                var cell = row.GetCell(columnIndexes["EmployeeEmail"]);
                dto.EmployeeEmail = GetStringValue(cell);
            }

            if (columnIndexes.ContainsKey("EmployeeId"))
            {
                var cell = row.GetCell(columnIndexes["EmployeeId"]);
                dto.EmployeeId = GetIntValue(cell);
            }

            if (columnIndexes.ContainsKey("Title"))
            {
                var cell = row.GetCell(columnIndexes["Title"]);
                dto.Title = GetStringValue(cell);
            }

            if (columnIndexes.ContainsKey("Description"))
            {
                var cell = row.GetCell(columnIndexes["Description"]);
                dto.Description = GetStringValue(cell);
            }

            if (columnIndexes.ContainsKey("CompletedAt"))
            {
                var cell = row.GetCell(columnIndexes["CompletedAt"]);
                dto.CompletedAt = GetDateTimeValue(cell);
            }

            if (columnIndexes.ContainsKey("ExternalSystemId"))
            {
                var cell = row.GetCell(columnIndexes["ExternalSystemId"]);
                dto.ExternalSystemId = GetStringValue(cell);
            }

            if (columnIndexes.ContainsKey("EfficiencyScore"))
            {
                var cell = row.GetCell(columnIndexes["EfficiencyScore"]);
                dto.EfficiencyScore = GetDoubleValue(cell);
            }

            if (columnIndexes.ContainsKey("Importance"))
            {
                var cell = row.GetCell(columnIndexes["Importance"]);
                dto.Importance = GetIntValue(cell) ?? 1;
            }

            return dto;
        }

        /// <summary>
        /// Создание DTO задачи из строки CSV
        /// </summary>
        private TaskRecordImportDto CreateTaskDtoFromCsv(string[] values, Dictionary<string, int> columnIndexes, int rowNumber)
        {
            if (!columnIndexes.ContainsKey("Title") || columnIndexes["Title"] >= values.Length)
            {
                // Если нет обязательного поля "Title", пропускаем строку
                return null;
            }

            var dto = new TaskRecordImportDto { RowNumber = rowNumber };

            // Заполняем поля DTO из значений
            if (columnIndexes.ContainsKey("EmployeeEmail") && columnIndexes["EmployeeEmail"] < values.Length)
            {
                dto.EmployeeEmail = values[columnIndexes["EmployeeEmail"]].Trim();
            }

            if (columnIndexes.ContainsKey("EmployeeId") && columnIndexes["EmployeeId"] < values.Length)
            {
                if (int.TryParse(values[columnIndexes["EmployeeId"]].Trim(), out int employeeId))
                {
                    dto.EmployeeId = employeeId;
                }
            }

            if (columnIndexes.ContainsKey("Title") && columnIndexes["Title"] < values.Length)
            {
                dto.Title = values[columnIndexes["Title"]].Trim();
            }

            if (columnIndexes.ContainsKey("Description") && columnIndexes["Description"] < values.Length)
            {
                dto.Description = values[columnIndexes["Description"]].Trim();
            }

            if (columnIndexes.ContainsKey("CompletedAt") && columnIndexes["CompletedAt"] < values.Length)
            {
                if (DateTime.TryParse(values[columnIndexes["CompletedAt"]].Trim(), out DateTime completedAt))
                {
                    dto.CompletedAt = completedAt;
                }
            }

            if (columnIndexes.ContainsKey("ExternalSystemId") && columnIndexes["ExternalSystemId"] < values.Length)
            {
                dto.ExternalSystemId = values[columnIndexes["ExternalSystemId"]].Trim();
            }

            if (columnIndexes.ContainsKey("EfficiencyScore") && columnIndexes["EfficiencyScore"] < values.Length)
            {
                if (double.TryParse(values[columnIndexes["EfficiencyScore"]].Trim(), out double efficiencyScore))
                {
                    dto.EfficiencyScore = efficiencyScore;
                }
            }

            if (columnIndexes.ContainsKey("Importance") && columnIndexes["Importance"] < values.Length)
            {
                if (int.TryParse(values[columnIndexes["Importance"]].Trim(), out int importance))
                {
                    dto.Importance = importance;
                }
            }

            return dto;
        }

        /// <summary>
        /// Получение строкового значения из ячейки Excel
        /// </summary>
        private string GetStringValue(ICell cell)
        {
            if (cell == null) return null;

            switch (cell.CellType)
            {
                case NPOI.SS.UserModel.CellType.String:
                    return cell.StringCellValue.Trim();
                case NPOI.SS.UserModel.CellType.Numeric:
                    return cell.NumericCellValue.ToString();
                case NPOI.SS.UserModel.CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Получение целочисленного значения из ячейки Excel
        /// </summary>
        private int? GetIntValue(ICell cell)
        {
            if (cell == null) return null;

            switch (cell.CellType)
            {
                case NPOI.SS.UserModel.CellType.Numeric:
                    return (int)cell.NumericCellValue;
                case NPOI.SS.UserModel.CellType.String:
                    if (int.TryParse(cell.StringCellValue.Trim(), out int value))
                        return value;
                    return null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Получение значения с плавающей точкой из ячейки Excel
        /// </summary>
        private double? GetDoubleValue(ICell cell)
        {
            if (cell == null) return null;

            switch (cell.CellType)
            {
                case NPOI.SS.UserModel.CellType.Numeric:
                    return cell.NumericCellValue;
                case NPOI.SS.UserModel.CellType.String:
                    if (double.TryParse(cell.StringCellValue.Trim(), out double value))
                        return value;
                    return null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Получение значения даты и времени из ячейки Excel
        /// </summary>
        private DateTime? GetDateTimeValue(ICell cell)
        {
            if (cell == null) return null;

            switch (cell.CellType)
            {
                case NPOI.SS.UserModel.CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                        return cell.DateCellValue;
                    return null;
                case NPOI.SS.UserModel.CellType.String:
                    if (DateTime.TryParse(cell.StringCellValue.Trim(), out DateTime value))
                        return value;
                    return null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Валидирует данные о задаче
        /// </summary>
        private Models.Import.ValidationResult ValidateTaskDto(TaskRecordImportDto dto)
        {
            var result = new Models.Import.ValidationResult { IsValid = true };

            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    RowNumber = dto.RowNumber,
                    FieldName = "Заголовок",
                    ErrorMessage = "Заголовок задачи обязателен для заполнения"
                });
            }

            // Должен быть указан хотя бы один идентификатор сотрудника (Email или ID)
            if (string.IsNullOrWhiteSpace(dto.EmployeeEmail) && !dto.EmployeeId.HasValue)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    RowNumber = dto.RowNumber,
                    FieldName = "Сотрудник",
                    ErrorMessage = "Должен быть указан хотя бы один идентификатор сотрудника (Email или ID)"
                });
            }

            // Проверка формата email
            if (!string.IsNullOrWhiteSpace(dto.EmployeeEmail))
            {
                if (!Regex.IsMatch(dto.EmployeeEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        RowNumber = dto.RowNumber,
                        FieldName = "Email",
                        ErrorMessage = "Неверный формат email"
                    });
                }
            }

            // Проверка эффективности (должна быть в диапазоне 0-100)
            if (dto.EfficiencyScore.HasValue && (dto.EfficiencyScore < 0 || dto.EfficiencyScore > 100))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    RowNumber = dto.RowNumber,
                    FieldName = "Эффективность",
                    ErrorMessage = "Эффективность должна быть в диапазоне от 0 до 100"
                });
            }

            // Проверка важности (должна быть в диапазоне 0-3)
            if (dto.Importance < 0 || dto.Importance > 3)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    RowNumber = dto.RowNumber,
                    FieldName = "Важность",
                    ErrorMessage = "Важность должна быть в диапазоне от 0 до 3"
                });
            }

            // Проверка даты выполнения
            if (dto.CompletedAt.HasValue && dto.CompletedAt > DateTime.Now)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    RowNumber = dto.RowNumber,
                    FieldName = "Дата выполнения",
                    ErrorMessage = "Дата выполнения не может быть в будущем"
                });
            }

            return result;
        }
    }
}