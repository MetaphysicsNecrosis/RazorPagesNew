using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using RazorPagesNew.Models.Import;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text.RegularExpressions;
using System.Xml;
using Mammoth;

namespace RazorPagesNew.Pages.Employees
{
    public class ImportModel : PageModel
    {
        private readonly MyApplicationDbContext _context;
        private readonly IEmployeeService _employeeService;
        private readonly IAuditLogService _auditLogService;
        private readonly IEmployeeImportService _importService;

        public ImportModel(
            MyApplicationDbContext context,
            IEmployeeService employeeService,
            IAuditLogService auditLogService,
            IEmployeeImportService importService)
        {
            _context = context;
            _employeeService = employeeService;
            _auditLogService = auditLogService;
            _importService = importService;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public IFormFile ImportFile { get; set; }

        [BindProperty]
        public int DefaultDepartmentId { get; set; }

        [BindProperty]
        public bool UpdateExisting { get; set; }

        [BindProperty]
        public bool SkipHeader { get; set; } = true;

        public SelectList DepartmentList { get; set; }

        public ImportResult ImportResult { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDepartmentsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ImportFile == null || ImportFile.Length == 0)
            {
                StatusMessage = "Ошибка: Файл не выбран.";
                await LoadDepartmentsAsync();
                return Page();
            }

            var defaultDepartment = await _context.Departments.FindAsync(DefaultDepartmentId);
            if (defaultDepartment == null)
            {
                StatusMessage = "Ошибка: Выберите корректный отдел по умолчанию.";
                await LoadDepartmentsAsync();
                return Page();
            }

            // Определение типа файла
            string extension = Path.GetExtension(ImportFile.FileName).ToLowerInvariant();

            // Подготовка результата импорта
            ImportResult = new ImportResult
            {
                FileName = ImportFile.FileName,
                FileType = extension,
                Success = true,
                AddedCount = 0,
                UpdatedCount = 0,
                ProcessedRows = 0,
                SuccessfulRows = 0,
                Message = "Импорт успешно завершен"
            };

            try
            {
                // Обработка файла в зависимости от типа
                List<EmployeeImportDto> employees = await ProcessImportFileAsync(extension);

                // Импорт данных в базу
                ImportResult = await _importService.ImportEmployeesAsync(
                    employees,
                    DefaultDepartmentId,
                    UpdateExisting,
                    User.Identity?.Name);
            }
            catch (Exception ex)
            {
                ImportResult.Success = false;
                ImportResult.Message = $"Ошибка при импорте: {ex.Message}";

                // Логирование ошибки
                await _auditLogService.LogActivityAsync(
                    User.Identity?.Name ?? "system",
                    Models.Enums.ActionType.Import,
                    "EmployeeImport",
                    "0",
                    $"Ошибка при импорте сотрудников: {ex.Message}"
                );
            }

            await LoadDepartmentsAsync();
            return Page();
        }

        private async Task<List<EmployeeImportDto>> ProcessImportFileAsync(string extension)
        {
            List<EmployeeImportDto> employees = new List<EmployeeImportDto>();

            // Обработка в зависимости от типа файла
            if (extension == ".xlsx" || extension == ".xls" || extension == ".csv")
            {
                employees = await ProcessExcelFileAsync();
            }
            else if (extension == ".docx" || extension == ".doc")
            {
                employees = await ProcessWordFileAsync();
            }
            else if (extension == ".xml")
            {
                employees = await Process1CXmlFileAsync();
            }
            else
            {
                throw new NotSupportedException($"Формат файла {extension} не поддерживается.");
            }

            return employees;
        }

        private async Task<List<EmployeeImportDto>> ProcessExcelFileAsync()
        {
            List<EmployeeImportDto> employees = new List<EmployeeImportDto>();

            // Сохраняем файл временно
            var filePath = Path.GetTempFileName();
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await ImportFile.CopyToAsync(stream);
            }

            try
            {
                using (SpreadsheetDocument document = SpreadsheetDocument.Open(filePath, false))
                {
                    WorkbookPart workbookPart = document.WorkbookPart;
                    WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                    Worksheet worksheet = worksheetPart.Worksheet;
                    SheetData sheetData = worksheet.GetFirstChild<SheetData>();

                    // Словарь для сопоставления индексов колонок с заголовками
                    Dictionary<int, string> columnMappings = new Dictionary<int, string>();

                    // Обрабатываем строки
                    int rowIndex = 0;
                    foreach (Row row in sheetData.Elements<Row>())
                    {
                        // Пропускаем заголовок, если указано
                        if (rowIndex == 0 && SkipHeader)
                        {
                            // Создаем маппинг колонок
                            int columnIndex = 0;
                            foreach (Cell cell in row.Elements<Cell>())
                            {
                                string cellValue = GetCellValue(cell, workbookPart);
                                columnMappings[columnIndex] = MapColumnName(cellValue);
                                columnIndex++;
                            }
                            rowIndex++;
                            continue;
                        }

                        // Если это первая строка и мы не пропускаем заголовок, создаем стандартный маппинг
                        if (rowIndex == 0 && !columnMappings.Any())
                        {
                            columnMappings[0] = "FullName";
                            columnMappings[1] = "Email";
                            columnMappings[2] = "Phone";
                            columnMappings[3] = "Position";
                            columnMappings[4] = "Department";
                            columnMappings[5] = "HireDate";
                        }

                        try
                        {
                            // Создаем объект сотрудника
                            var employee = new EmployeeImportDto() { RowNumber = rowIndex + 1 };
                            bool hasData = false;

                            // Обрабатываем ячейки
                            int cellIndex = 0;
                            foreach (Cell cell in row.Elements<Cell>())
                            {
                                string cellValue = GetCellValue(cell, workbookPart);
                                if (!string.IsNullOrWhiteSpace(cellValue))
                                {
                                    hasData = true;

                                    // Определяем, какой столбец обрабатываем
                                    if (columnMappings.TryGetValue(cellIndex, out string columnName))
                                    {
                                        SetEmployeeProperty(employee, columnName, cellValue);
                                    }
                                }
                                cellIndex++;
                            }

                            // Добавляем сотрудника, если есть данные
                            if (hasData)
                            {
                                employees.Add(employee);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Добавляем ошибку в результаты
                            ImportResult.Errors.Add(new ImportError
                            {
                                RowNumber = rowIndex + 1,
                                FieldName = "Строка целиком",
                                ErrorMessage = ex.Message
                            });
                        }

                        rowIndex++;
                    }
                }
            }
            finally
            {
                // Удаляем временный файл
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            return employees;
        }

        private async Task<List<EmployeeImportDto>> ProcessWordFileAsync()
        {
            List<EmployeeImportDto> employees = new List<EmployeeImportDto>();

            // Сохраняем файл временно
            var filePath = Path.GetTempFileName();
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await ImportFile.CopyToAsync(stream);
            }

            try
            {
                // Используем Mammoth для преобразования Word в HTML
                var result = new DocumentConverter()
                    .ConvertToHtml(filePath);

                string html = result.Value; // HTML представление документа

                // Используем HtmlAgilityPack для парсинга HTML
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);

                // Проверяем наличие таблиц
                var tables = doc.DocumentNode.SelectNodes("//table");
                if (tables != null && tables.Count > 0)
                {
                    // Обрабатываем первую таблицу
                    var table = tables[0];
                    var rows = table.SelectNodes(".//tr");

                    // Словарь для сопоставления индексов колонок с заголовками
                    Dictionary<int, string> columnMappings = new Dictionary<int, string>();

                    // Обрабатываем строки таблицы
                    for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                    {
                        var row = rows[rowIndex];
                        var cells = row.SelectNodes(".//td|.//th");

                        // Пропускаем заголовок, если указано
                        if (rowIndex == 0 && SkipHeader)
                        {
                            // Создаем маппинг колонок из заголовков
                            if (cells != null)
                            {
                                for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                                {
                                    string cellValue = cells[cellIndex].InnerText.Trim();
                                    columnMappings[cellIndex] = MapColumnName(cellValue);
                                }
                            }
                            continue;
                        }

                        // Если первая строка и нет маппинга, создаем стандартный
                        if (rowIndex == 0 && !columnMappings.Any())
                        {
                            columnMappings[0] = "FullName";
                            columnMappings[1] = "Email";
                            columnMappings[2] = "Phone";
                            columnMappings[3] = "Position";
                            columnMappings[4] = "Department";
                            columnMappings[5] = "HireDate";
                        }

                        if (cells != null && cells.Count > 0)
                        {
                            try
                            {
                                // Создаем объект сотрудника
                                var employee = new EmployeeImportDto() { RowNumber = rowIndex + 1 };
                                bool hasData = false;

                                // Обрабатываем ячейки
                                for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                                {
                                    string cellValue = cells[cellIndex].InnerText.Trim();
                                    if (!string.IsNullOrWhiteSpace(cellValue))
                                    {
                                        hasData = true;

                                        // Определяем, какой столбец обрабатываем
                                        if (columnMappings.TryGetValue(cellIndex, out string columnName))
                                        {
                                            SetEmployeeProperty(employee, columnName, cellValue);
                                        }
                                    }
                                }

                                // Добавляем сотрудника, если есть данные
                                if (hasData)
                                {
                                    employees.Add(employee);
                                }
                            }
                            catch (Exception ex)
                            {
                                // Добавляем ошибку в результаты
                                ImportResult.Errors.Add(new ImportError
                                {
                                    RowNumber = rowIndex + 1,
                                    FieldName = "Строка таблицы",
                                    ErrorMessage = ex.Message
                                });
                            }
                        }
                    }
                }
                else
                {
                    // Если таблиц нет, пробуем найти структурированный текст
                    // Ищем паттерны вида "Поле: Значение"
                    var paragraphs = doc.DocumentNode.SelectNodes("//p");
                    if (paragraphs != null)
                    {
                        var employee = new EmployeeImportDto() { RowNumber = 1 };
                        bool hasData = false;

                        foreach (var paragraph in paragraphs)
                        {
                            string text = paragraph.InnerText.Trim();
                            if (!string.IsNullOrEmpty(text))
                            {
                                // Ищем паттерн "Поле: Значение"
                                var match = Regex.Match(text, @"^([^:]+):\s*(.+)$");
                                if (match.Success)
                                {
                                    string fieldName = match.Groups[1].Value.Trim();
                                    string fieldValue = match.Groups[2].Value.Trim();

                                    string columnName = MapColumnName(fieldName);
                                    if (!string.IsNullOrEmpty(columnName))
                                    {
                                        SetEmployeeProperty(employee, columnName, fieldValue);
                                        hasData = true;
                                    }
                                }

                                // Если это отметка о новом сотруднике
                                if (text.Contains("Сотрудник:") || text.Contains("Новый сотрудник") ||
                                    text.Contains("---") && hasData)
                                {
                                    if (hasData)
                                    {
                                        employees.Add(employee);
                                        employee = new EmployeeImportDto() { RowNumber = employees.Count + 1 };
                                        hasData = false;
                                    }
                                }
                            }
                        }

                        // Добавляем последнего сотрудника, если есть данные
                        if (hasData)
                        {
                            employees.Add(employee);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при обработке Word файла: {ex.Message}", ex);
            }
            finally
            {
                // Удаляем временный файл
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            return employees;
        }

        private async Task<List<EmployeeImportDto>> Process1CXmlFileAsync()
        {
            List<EmployeeImportDto> employees = new List<EmployeeImportDto>();

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await ImportFile.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(memoryStream);

                    // Пытаемся найти узлы сотрудников в различных форматах 1C
                    XmlNodeList employeeNodes = null;

                    // Попытка найти узлы в разных форматах
                    string[] nodePatterns = {
                        "//Сотрудники/Сотрудник",
                        "//Справочник.Сотрудники/Сотрудник",
                        "//Catalog.Сотрудники/Item",
                        "//Document/Employees/Employee"
                    };

                    foreach (var pattern in nodePatterns)
                    {
                        employeeNodes = xmlDoc.SelectNodes(pattern);
                        if (employeeNodes != null && employeeNodes.Count > 0)
                            break;
                    }

                    if (employeeNodes == null || employeeNodes.Count == 0)
                    {
                        throw new Exception("Не найдены данные о сотрудниках в XML файле. Проверьте формат файла.");
                    }

                    // Определим возможные имена атрибутов/элементов для полей сотрудника
                    Dictionary<string, string[]> fieldMapping = new Dictionary<string, string[]>
                    {
                        ["FullName"] = new[] { "ФИО", "Наименование", "Name", "ПолноеИмя", "Description" },
                        ["Email"] = new[] { "Email", "Эл_почта", "EmailAddress", "АдресЭлектроннойПочты" },
                        ["Phone"] = new[] { "Телефон", "Phone", "КонтактныйТелефон", "ТелефонРабочий", "PhoneNumber" },
                        ["Position"] = new[] { "Должность", "Position", "ДолжностьПоШтату", "JobTitle" },
                        ["Department"] = new[] { "Отдел", "Подразделение", "Department", "Division" },
                        ["HireDate"] = new[] { "ДатаПриема", "HireDate", "ДатаПриемаНаРаботу" }
                    };

                    int rowNumber = 1;
                    foreach (XmlNode employeeNode in employeeNodes)
                    {
                        try
                        {
                            var employee = new EmployeeImportDto { RowNumber = rowNumber };
                            bool hasData = false;

                            // Для каждого поля ищем соответствующие атрибуты или дочерние элементы
                            foreach (var field in fieldMapping)
                            {
                                string value = null;

                                // Сначала проверяем атрибуты
                                foreach (var fieldName in field.Value)
                                {
                                    if (employeeNode.Attributes?[fieldName] != null)
                                    {
                                        value = employeeNode.Attributes[fieldName].Value;
                                        break;
                                    }
                                }

                                // Если не нашли в атрибутах, ищем в дочерних элементах
                                if (value == null)
                                {
                                    foreach (var fieldName in field.Value)
                                    {
                                        var childNode = employeeNode.SelectSingleNode($"./{fieldName}");
                                        if (childNode != null)
                                        {
                                            value = childNode.InnerText;
                                            break;
                                        }
                                    }
                                }

                                // Устанавливаем значение поля, если нашли
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    SetEmployeeProperty(employee, field.Key, value);
                                    hasData = true;
                                }
                            }

                            // Добавляем сотрудника, если нашли хоть какие-то данные
                            if (hasData)
                            {
                                employees.Add(employee);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Добавляем ошибку в результаты
                            ImportResult.Errors.Add(new ImportError
                            {
                                RowNumber = rowNumber,
                                FieldName = "XML узел",
                                ErrorMessage = ex.Message
                            });
                        }

                        rowNumber++;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при обработке XML файла: {ex.Message}", ex);
            }

            return employees;
        }

        private void SetEmployeeProperty(EmployeeImportDto employee, string propertyName, string value)
        {
            switch (propertyName)
            {
                case "FullName":
                    employee.FullName = value;
                    break;

                case "Email":
                    employee.Email = value;
                    break;

                case "Phone":
                    employee.Phone = value;
                    break;

                case "Position":
                    employee.Position = value;
                    break;

                case "Department":
                    employee.Department = value;
                    break;

                case "HireDate":
                    if (DateTime.TryParse(value, out DateTime hireDate))
                    {
                        employee.HireDate = hireDate;
                    }
                    break;
            }
        }

        private string MapColumnName(string headerName)
        {
            if (string.IsNullOrWhiteSpace(headerName))
                return null;

            // Приводим к нижнему регистру и очищаем от лишних символов
            headerName = headerName.ToLowerInvariant().Trim();

            // Маппинг заголовков на имена свойств
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Русские названия
                { "фио", "FullName" },
                { "имя", "FullName" },
                { "полное имя", "FullName" },
                { "сотрудник", "FullName" },
                { "email", "Email" },
                { "почта", "Email" },
                { "эл. почта", "Email" },
                { "электронная почта", "Email" },
                { "телефон", "Phone" },
                { "тел", "Phone" },
                { "тел.", "Phone" },
                { "контактный телефон", "Phone" },
                { "должность", "Position" },
                { "позиция", "Position" },
                { "отдел", "Department" },
                { "подразделение", "Department" },
                { "дата приема", "HireDate" },
                { "дата", "HireDate" },
                { "принят с", "HireDate" },
                
                // Английские названия
                { "name", "FullName" },
                { "full name", "FullName" },
                { "employee", "FullName" },
                { "employee name", "FullName" },
                { "mail", "Email" },
                { "e-mail", "Email" },
                { "emailaddress", "Email" },
                { "phone", "Phone" },
                { "phone number", "Phone" },
                { "tel", "Phone" },
                { "telephone", "Phone" },
                { "position", "Position" },
                { "job title", "Position" },
                { "title", "Position" },
                { "department", "Department" },
                { "division", "Department" },
                { "hire date", "HireDate" },
                { "hired", "HireDate" },
                { "employment date", "HireDate" }
            };

            foreach (var mapping in mappings)
            {
                if (headerName.Contains(mapping.Key))
                {
                    return mapping.Value;
                }
            }

            return null;
        }

        private string GetCellValue(Cell cell, WorkbookPart workbookPart)
        {
            if (cell == null)
                return string.Empty;

            // Получаем значение ячейки
            string value = cell.InnerText;

            // Если ячейка содержит ссылку на SharedString
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                int ssid = int.Parse(value);
                SharedStringItem ssi = workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(ssid);
                value = ssi.Text?.Text ?? ssi.InnerText ?? string.Empty;
            }

            return value;
        }

        private async Task LoadDepartmentsAsync()
        {
            var departments = await _employeeService.GetAllDepartmentsAsync();
            DepartmentList = new SelectList(departments, "Id", "Name");
        }
    }
}