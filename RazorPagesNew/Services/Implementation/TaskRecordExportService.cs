using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Linq;
using System.Text;
using RazorPagesNew.Models.Import;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RazorPagesNew.Services.Implementation
{
    public class TaskRecordExportService : ITaskRecordExportService
    {
        private readonly MyApplicationDbContext _context;

        public TaskRecordExportService(MyApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Экспортирует задачи в формат Excel (XLSX)
        /// </summary>
        public async Task<byte[]> ExportToExcelAsync(
            IEnumerable<TaskRecordExportDto> tasks,
            string sheetName = "Задачи")
        {
            // Создаем новую книгу Excel
            IWorkbook workbook = new XSSFWorkbook();

            // Создаем лист с указанным именем
            ISheet sheet = workbook.CreateSheet(sheetName);

            // Создаем стиль для заголовков
            ICellStyle headerStyle = workbook.CreateCellStyle();
            headerStyle.FillForegroundColor = IndexedColors.Grey25Percent.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;

            var headerFont = workbook.CreateFont();
            headerFont.Boldweight = (short)FontBoldWeight.Bold;
            headerStyle.SetFont(headerFont);

            // Создаем заголовки
            IRow headerRow = sheet.CreateRow(0);

            string[] headers = new string[]
            {
                "ID задачи", "ID сотрудника", "Сотрудник", "Email", "Отдел",
                "Заголовок", "Описание", "Дата выполнения", "Внешний ID",
                "Эффективность", "Важность", "Создатель", "Дата создания"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ICell cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;

                // Автоподбор ширины колонок
                sheet.AutoSizeColumn(i);
            }

            // Заполняем данными
            int rowIndex = 1;

            foreach (var task in tasks)
            {
                IRow row = sheet.CreateRow(rowIndex++);

                row.CreateCell(0).SetCellValue(task.Id);
                row.CreateCell(1).SetCellValue(task.EmployeeId);
                row.CreateCell(2).SetCellValue(task.EmployeeName);
                row.CreateCell(3).SetCellValue(task.EmployeeEmail);
                row.CreateCell(4).SetCellValue(task.DepartmentName);
                row.CreateCell(5).SetCellValue(task.Title);
                row.CreateCell(6).SetCellValue(task.Description);
                row.CreateCell(7).SetCellValue(task.CompletedAt.ToString("dd.MM.yyyy HH:mm"));
                row.CreateCell(8).SetCellValue(task.ExternalSystemId);

                if (task.EfficiencyScore.HasValue)
                {
                    row.CreateCell(9).SetCellValue(task.EfficiencyScore.Value);
                }
                else
                {
                    row.CreateCell(9).SetCellValue("");
                }

                row.CreateCell(10).SetCellValue(GetImportanceText(task.Importance));
                row.CreateCell(11).SetCellValue(task.OwnerUsername);
                row.CreateCell(12).SetCellValue(task.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
            }

            // Автоподбор ширины колонок
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            // Возвращаем как массив байтов
            using (var memoryStream = new MemoryStream())
            {
                workbook.Write(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Экспортирует задачи в формат CSV
        /// </summary>
        public async Task<byte[]> ExportToCsvAsync(
            IEnumerable<TaskRecordExportDto> tasks,
            string delimiter = ",",
            bool includeHeader = true)
        {
            StringBuilder csvContent = new StringBuilder();

            // Добавляем заголовки
            if (includeHeader)
            {
                csvContent.AppendLine(string.Join(delimiter,
                    "ID задачи", "ID сотрудника", "Сотрудник", "Email", "Отдел",
                    "Заголовок", "Описание", "Дата выполнения", "Внешний ID",
                    "Эффективность", "Важность", "Создатель", "Дата создания"));
            }

            // Добавляем данные
            foreach (var task in tasks)
            {
                // Экранируем поля, содержащие разделитель или переводы строк
                string[] fields =
                {
                    task.Id.ToString(),
                    task.EmployeeId.ToString(),
                    EscapeCsvField(task.EmployeeName, delimiter),
                    EscapeCsvField(task.EmployeeEmail, delimiter),
                    EscapeCsvField(task.DepartmentName, delimiter),
                    EscapeCsvField(task.Title, delimiter),
                    EscapeCsvField(task.Description, delimiter),
                    task.CompletedAt.ToString("dd.MM.yyyy HH:mm"),
                    EscapeCsvField(task.ExternalSystemId, delimiter),
                    task.EfficiencyScore?.ToString() ?? "",
                    GetImportanceText(task.Importance),
                    EscapeCsvField(task.OwnerUsername, delimiter),
                    task.CreatedAt.ToString("dd.MM.yyyy HH:mm")
                };

                csvContent.AppendLine(string.Join(delimiter, fields));
            }

            // Преобразуем в массив байтов с кодировкой UTF-8 (с BOM)
            return Encoding.UTF8.GetBytes(csvContent.ToString());
        }

        /// <summary>
        /// Конвертирует список задач в DTO для экспорта
        /// </summary>
        public async Task<IEnumerable<TaskRecordExportDto>> ConvertToExportDtoAsync(
            IEnumerable<ModelsDb.TaskRecord> tasks)
        {
            var result = new List<TaskRecordExportDto>();

            // Получаем все необходимые данные для задач
            var employeeIds = tasks.Select(t => t.EmployeeId).Distinct().ToList();
            var ownerIds = tasks.Select(t => t.OwnerId).Distinct().ToList();

            // Загружаем связанные данные
            var employees = await _context.Employees
                .Where(e => employeeIds.Contains(e.Id))
                .Include(e => e.Department)
                .ToDictionaryAsync(e => e.Id, e => e);

            var owners = await _context.Users
                .Where(u => ownerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            // Конвертируем задачи в DTO
            foreach (var task in tasks)
            {
                var dto = new TaskRecordExportDto
                {
                    Id = task.Id,
                    EmployeeId = task.EmployeeId,
                    Title = task.Title,
                    Description = task.Description ?? "",
                    CompletedAt = task.CompletedAt,
                    ExternalSystemId = task.ExternalSystemId ?? "",
                    EfficiencyScore = task.EfficiencyScore,
                    Importance = task.Importance,
                    CreatedAt = task.CreatedAt
                };

                // Добавляем данные о сотруднике
                if (employees.TryGetValue(task.EmployeeId, out var employee))
                {
                    dto.EmployeeName = employee.FullName;
                    dto.EmployeeEmail = employee.Email;
                    dto.DepartmentName = employee.Department?.Name ?? "Нет отдела";
                }
                else
                {
                    dto.EmployeeName = "Неизвестный сотрудник";
                    dto.EmployeeEmail = "";
                    dto.DepartmentName = "Нет отдела";
                }

                // Добавляем данные о создателе
                if (owners.TryGetValue(task.OwnerId, out var owner))
                {
                    dto.OwnerUsername = owner.Username;
                }
                else
                {
                    dto.OwnerUsername = "Система";
                }

                result.Add(dto);
            }

            return result;
        }

        /// <summary>
        /// Экспортирует задачи в PDF формат
        /// </summary>
        public async Task<byte[]> ExportToPdfAsync(
            IEnumerable<TaskRecordExportDto> tasks,
            string title = "Отчет по задачам")
        {
            using (var memoryStream = new MemoryStream())
            {
                // Создаем документ PDF
                Document document = new Document(PageSize.A4.Rotate(), 20, 20, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);

                document.Open();

                // Добавляем заголовок отчета
                Font titleFont = new Font(Font.FontFamily.HELVETICA, 16, Font.BOLD);
                Paragraph titleParagraph = new Paragraph(title, titleFont);
                titleParagraph.Alignment = Element.ALIGN_CENTER;
                titleParagraph.SpacingAfter = 20;
                document.Add(titleParagraph);

                // Добавляем дату и время создания отчета
                Font normalFont = new Font(Font.FontFamily.HELVETICA, 10, Font.NORMAL);
                Paragraph dateParagraph = new Paragraph($"Дата создания: {DateTime.Now:dd.MM.yyyy HH:mm}", normalFont);
                dateParagraph.Alignment = Element.ALIGN_RIGHT;
                dateParagraph.SpacingAfter = 20;
                document.Add(dateParagraph);

                // Создаем таблицу
                PdfPTable table = new PdfPTable(9);
                table.WidthPercentage = 100;

                // Задаем относительную ширину колонок
                float[] columnWidths = new float[] { 1f, 3f, 3f, 2f, 3.5f, 2f, 1.5f, 1.5f, 2f };
                table.SetWidths(columnWidths);

                // Добавляем заголовки
                Font headerFont = new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD);
                string[] headers = new string[]
                {
                    "ID", "Сотрудник", "Отдел", "Дата", "Задача", "Внешний ID",
                    "Эффект. %", "Важность", "Создатель"
                };

                foreach (var header in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, headerFont));
                    cell.BackgroundColor = new BaseColor(220, 220, 220);
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    cell.Padding = 5;
                    table.AddCell(cell);
                }

                // Добавляем данные
                Font cellFont = new Font(Font.FontFamily.HELVETICA, 8, Font.NORMAL);

                foreach (var task in tasks)
                {
                    // ID
                    AddCell(table, task.Id.ToString(), cellFont);

                    // Сотрудник
                    AddCell(table, $"{task.EmployeeName}\n{task.EmployeeEmail}", cellFont);

                    // Отдел
                    AddCell(table, task.DepartmentName, cellFont);

                    // Дата выполнения
                    AddCell(table, task.CompletedAt.ToString("dd.MM.yyyy"), cellFont);

                    // Задача (Заголовок + Описание)
                    string taskText = task.Title;
                    if (!string.IsNullOrEmpty(task.Description))
                    {
                        // Сокращаем описание, если оно слишком длинное
                        string description = task.Description.Length > 100
                            ? task.Description.Substring(0, 100) + "..."
                            : task.Description;
                        taskText += "\n" + description;
                    }
                    AddCell(table, taskText, cellFont);

                    // Внешний ID
                    AddCell(table, task.ExternalSystemId, cellFont);

                    // Эффективность
                    string efficiency = task.EfficiencyScore.HasValue
                        ? task.EfficiencyScore.Value.ToString("F1")
                        : "-";
                    AddCell(table, efficiency, cellFont, Element.ALIGN_CENTER);

                    // Важность
                    AddCell(table, GetImportanceText(task.Importance), cellFont, Element.ALIGN_CENTER);

                    // Создатель
                    AddCell(table, task.OwnerUsername, cellFont);
                }

                document.Add(table);

                // Добавляем подпись
                Paragraph footerParagraph = new Paragraph($"Всего задач: {tasks.Count()}", normalFont);
                footerParagraph.Alignment = Element.ALIGN_RIGHT;
                footerParagraph.SpacingBefore = 20;
                document.Add(footerParagraph);

                document.Close();

                return memoryStream.ToArray();
            }
        }

        #region Вспомогательные методы

        /// <summary>
        /// Экранирует поле CSV, если оно содержит разделитель или переводы строк
        /// </summary>
        private string EscapeCsvField(string field, string delimiter)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // Заменяем двойные кавычки на две двойные кавычки (экранирование для CSV)
            field = field.Replace("\"", "\"\"");

            // Если поле содержит разделитель, новую строку или кавычки, то заключаем его в кавычки
            if (field.Contains(delimiter) || field.Contains("\n") || field.Contains("\r") || field.Contains("\""))
            {
                field = $"\"{field}\"";
            }

            return field;
        }

        /// <summary>
        /// Возвращает текстовое представление важности задачи
        /// </summary>
        private string GetImportanceText(int importance)
        {
            return importance switch
            {
                0 => "Низкая",
                1 => "Средняя",
                2 => "Высокая",
                3 => "Критическая",
                _ => "Неизвестная"
            };
        }

        /// <summary>
        /// Добавляет ячейку в таблицу PDF
        /// </summary>
        private void AddCell(PdfPTable table, string text, Font font, int alignment = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text ?? "", font));
            cell.HorizontalAlignment = alignment;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.Padding = 4;
            cell.MinimumHeight = 20;
            table.AddCell(cell);
        }

        #endregion
    }
}