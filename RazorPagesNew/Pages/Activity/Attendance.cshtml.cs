using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System.Text;
using System.Globalization;
using OfficeOpenXml;
using System.Drawing;
namespace RazorPagesNew.Pages.Activity
{
    public class AttendanceModel : PageModel
    {
        private readonly MyApplicationDbContext _context;
        private readonly IEmployeeService _employeeService;
        private readonly IUserService _userService;
        public AttendanceModel(
            MyApplicationDbContext context,
            IEmployeeService employeeService,
            IUserService userService)
        {
            /*ExcelPackage.LicenseContext = LicenseContext.NonCommercial;*/
            _context = context;
            _employeeService = employeeService;
            _userService = userService;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public string CurrentFilter { get; set; }
        public string CurrentSort { get; set; }
        public int? EmployeeId { get; set; }
        public SelectList EmployeeList { get; set; }
        public string NameSort { get; set; }
        public string DateSort { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
        public List<Employee> AllEmployees { get; set; } = new List<Employee>();
        public List<EmployeeAttendanceStats> EmployeeAttendanceStats { get; set; } = new List<EmployeeAttendanceStats>();

        // Данные для календаря
        public List<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();

        // Данные для графиков
        public List<string> ChartDates { get; set; } = new List<string>();
        public List<int> ChartCounts { get; set; } = new List<int>();
        public List<double> AvgCheckInTimes { get; set; } = new List<double>();
        public List<double> AvgCheckOutTimes { get; set; } = new List<double>();

        public async Task<IActionResult> OnGetAsync(
            string sortOrder,
            string currentFilter,
            string searchTerm,
            int? employeeId,
            DateTime? startDate,
            DateTime? endDate,
            int? pageIndex)
        {
            // Проверка аутентификации
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // Сохраняем параметры фильтрации
            CurrentSort = sortOrder;
            EmployeeId = employeeId;
            CurrentPage = pageIndex ?? 1;

            // Устанавливаем даты по умолчанию, если не указаны
            StartDate = startDate ?? DateTime.Today.AddDays(-30);
            EndDate = endDate ?? DateTime.Today;

            // Устанавливаем направление сортировки
            DateSort = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            NameSort = sortOrder == "name" ? "name_desc" : "name";

            // Если новый поисковый запрос, возвращаемся на первую страницу
            if (searchTerm != null)
            {
                CurrentPage = 1;
            }
            else
            {
                searchTerm = currentFilter;
            }

            CurrentFilter = searchTerm;

            // Загружаем список сотрудников для фильтрации
            await LoadEmployeesAsync();

            // Получаем записи о посещаемости по фильтрам
            var attendanceQuery = _context.AttendanceRecords
                .Include(a => a.Employee)
                .Where(a => a.Date >= StartDate && a.Date <= EndDate);

            if (EmployeeId.HasValue)
            {
                attendanceQuery = attendanceQuery.Where(a => a.EmployeeId == EmployeeId.Value);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                attendanceQuery = attendanceQuery.Where(a => a.Employee.FullName.Contains(searchTerm));
            }

            // Сортировка результатов
            switch (sortOrder)
            {
                case "date_desc":
                    attendanceQuery = attendanceQuery.OrderByDescending(a => a.Date);
                    break;
                case "name":
                    attendanceQuery = attendanceQuery.OrderBy(a => a.Employee.FullName);
                    break;
                case "name_desc":
                    attendanceQuery = attendanceQuery.OrderByDescending(a => a.Employee.FullName);
                    break;
                default:
                    attendanceQuery = attendanceQuery.OrderBy(a => a.Date);
                    break;
            }

            // Получаем общее количество записей
            var totalItems = await attendanceQuery.CountAsync();

            // Пагинация
            int pageSize = 10;
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            AttendanceRecords = await attendanceQuery
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Подготовка данных для календаря
            await PrepareCalendarDataAsync();

            // Подготовка данных для графиков
            await PrepareChartDataAsync();

            // Подготовка статистики по сотрудникам
            await PrepareEmployeeStatsAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync(int employeeId, DateTime date, string checkIn, string checkOut)
        {
            // Проверка аутентификации
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // Базовая валидация
            if (employeeId <= 0 || date == default || string.IsNullOrEmpty(checkIn) || string.IsNullOrEmpty(checkOut))
            {
                StatusMessage = "Ошибка: Некорректные данные. Пожалуйста, заполните все поля.";
                return RedirectToPage();
            }

            try
            {
                // Парсим время
                var checkInTime = DateTime.Parse(date.ToShortDateString() + " " + checkIn);
                var checkOutTime = DateTime.Parse(date.ToShortDateString() + " " + checkOut);

                // Проверяем, что время ухода позже времени прихода
                if (checkOutTime <= checkInTime)
                {
                    StatusMessage = "Ошибка: Время ухода должно быть позже времени прихода.";
                    return RedirectToPage();
                }

                // Рассчитываем количество отработанных часов
                int hoursWorked = (int)Math.Round((checkOutTime - checkInTime).TotalHours);

                // Создаем новую запись
                var attendanceRecord = new AttendanceRecord
                {
                    EmployeeId = employeeId,
                    Date = date.Date,
                    CheckIn = checkInTime,
                    CheckOut = checkOutTime,
                    HoursWorked = hoursWorked,
                    SourceSystem = "Manual",
                    CreatedAt = DateTime.UtcNow
                };

                // Проверяем, не существует ли уже запись на эту дату для данного сотрудника
                var existingRecord = await _context.AttendanceRecords
                    .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == date.Date);

                if (existingRecord != null)
                {
                    StatusMessage = $"Ошибка: Запись о посещаемости для данного сотрудника на {date.ToShortDateString()} уже существует.";
                    return RedirectToPage();
                }

                // Сохраняем запись
                await _context.AttendanceRecords.AddAsync(attendanceRecord);
                await _context.SaveChangesAsync();

                StatusMessage = $"Запись о посещаемости на {date.ToShortDateString()} успешно создана.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(int id, int employeeId, DateTime date, string checkIn, string checkOut)
        {
            // Проверка аутентификации
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // Базовая валидация
            if (id <= 0 || employeeId <= 0 || date == default || string.IsNullOrEmpty(checkIn) || string.IsNullOrEmpty(checkOut))
            {
                StatusMessage = "Ошибка: Некорректные данные. Пожалуйста, заполните все поля.";
                return RedirectToPage();
            }

            try
            {
                // Получаем существующую запись
                var attendanceRecord = await _context.AttendanceRecords.FindAsync(id);
                if (attendanceRecord == null)
                {
                    StatusMessage = "Ошибка: Запись не найдена.";
                    return RedirectToPage();
                }

                // Парсим время
                var checkInTime = DateTime.Parse(date.ToShortDateString() + " " + checkIn);
                var checkOutTime = DateTime.Parse(date.ToShortDateString() + " " + checkOut);

                // Проверяем, что время ухода позже времени прихода
                if (checkOutTime <= checkInTime)
                {
                    StatusMessage = "Ошибка: Время ухода должно быть позже времени прихода.";
                    return RedirectToPage();
                }

                // Рассчитываем количество отработанных часов
                int hoursWorked = (int)Math.Round((checkOutTime - checkInTime).TotalHours);

                // Обновляем запись
                attendanceRecord.Date = date.Date;
                attendanceRecord.CheckIn = checkInTime;
                attendanceRecord.CheckOut = checkOutTime;
                attendanceRecord.HoursWorked = hoursWorked;
                attendanceRecord.UpdatedAt = DateTime.UtcNow;

                _context.AttendanceRecords.Update(attendanceRecord);
                await _context.SaveChangesAsync();

                StatusMessage = $"Запись о посещаемости на {date.ToShortDateString()} успешно обновлена.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            // Проверка аутентификации
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // Базовая валидация
            if (id <= 0)
            {
                StatusMessage = "Ошибка: Некорректный идентификатор записи.";
                return RedirectToPage();
            }

            try
            {
                // Получаем существующую запись
                var attendanceRecord = await _context.AttendanceRecords.FindAsync(id);
                if (attendanceRecord == null)
                {
                    StatusMessage = "Ошибка: Запись не найдена.";
                    return RedirectToPage();
                }

                // Удаляем запись
                _context.AttendanceRecords.Remove(attendanceRecord);
                await _context.SaveChangesAsync();

                StatusMessage = "Запись о посещаемости успешно удалена.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            // Проверка аутентификации
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            try
            {
                // Получаем все записи о посещаемости
                var records = await _context.AttendanceRecords
                    .Include(a => a.Employee)
                    .OrderBy(a => a.Date)
                    .ThenBy(a => a.Employee.FullName)
                    .ToListAsync();

                // Настройка лицензии EPPlus
                ExcelPackage.License.SetNonCommercialPersonal("Anon");
                // Создаем новый пакет Excel
                using (var package = new ExcelPackage())
                {
                    // Добавляем лист
                    var worksheet = package.Workbook.Worksheets.Add("Посещаемость");

                    // Стиль заголовка
                    var headerStyle = worksheet.Cells["A1:G1"].Style;
                    headerStyle.Font.Bold = true;
                    headerStyle.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerStyle.Fill.BackgroundColor.SetColor(Color.LightGray);
                    headerStyle.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    // Добавляем заголовки
                    worksheet.Cells[1, 1].Value = "ID сотрудника";
                    worksheet.Cells[1, 2].Value = "ФИО сотрудника";
                    worksheet.Cells[1, 3].Value = "Дата";
                    worksheet.Cells[1, 4].Value = "Время прихода";
                    worksheet.Cells[1, 5].Value = "Время ухода";
                    worksheet.Cells[1, 6].Value = "Часы работы";
                    worksheet.Cells[1, 7].Value = "Опоздание";

                    // Добавляем данные
                    int row = 2;
                    foreach (var record in records)
                    {
                        worksheet.Cells[row, 1].Value = record.EmployeeId;
                        worksheet.Cells[row, 2].Value = record.Employee.FullName;
                        worksheet.Cells[row, 3].Value = record.Date;
                        worksheet.Cells[row, 3].Style.Numberformat.Format = "dd.MM.yyyy";
                        worksheet.Cells[row, 4].Value = record.CheckIn;
                        worksheet.Cells[row, 4].Style.Numberformat.Format = "HH:mm";
                        worksheet.Cells[row, 5].Value = record.CheckOut;
                        worksheet.Cells[row, 5].Style.Numberformat.Format = "HH:mm";
                        worksheet.Cells[row, 6].Value = record.HoursWorked;

                        bool isLate = record.CheckIn.TimeOfDay > new TimeSpan(9, 0, 0);
                        worksheet.Cells[row, 7].Value = isLate ? "Да" : "Нет";
                        if (isLate)
                        {
                            worksheet.Cells[row, 7].Style.Font.Color.SetColor(Color.Red);
                        }

                        row++;
                    }

                    // Автоматическая настройка ширины столбцов
                    worksheet.Cells.AutoFitColumns();

                    // Возвращаем файл
                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    string fileName = $"Посещаемость_{DateTime.Now.ToString("yyyy-MM-dd")}.xlsx";
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при экспорте данных: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostImportAsync(IFormFile importFile)
        {
            // Проверка аутентификации
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            if (importFile == null || importFile.Length == 0)
            {
                StatusMessage = "Ошибка: Файл не выбран или пуст.";
                return RedirectToPage();
            }

            try
            {
                // Получаем текущего пользователя
                var currentUser = await _userService.GetCurrentUserAsync(User);
                if (currentUser == null)
                    return RedirectToPage("/Account/Login");

                // Проверяем расширение файла
                var fileExtension = Path.GetExtension(importFile.FileName).ToLower();
                if (fileExtension != ".csv" && fileExtension != ".xlsx" && fileExtension != ".xls")
                {
                    StatusMessage = "Ошибка: Поддерживаются только файлы CSV и Excel.";
                    return RedirectToPage();
                }

                int importedCount = 0;
                int errorCount = 0;

                // Открываем файл
                using (var stream = importFile.OpenReadStream())
                {
                    if (fileExtension == ".csv")
                    {
                        // Обрабатываем CSV
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            string line;
                            bool isFirstLine = true;

                            while ((line = reader.ReadLine()) != null)
                            {
                                if (isFirstLine)
                                {
                                    isFirstLine = false;
                                    continue; // Пропускаем заголовок
                                }

                                var parts = line.Split(',').Select(p => p.Trim()).ToArray();
                                if (parts.Length < 4)
                                    continue;

                                try
                                {
                                    int employeeId = int.Parse(parts[0]);
                                    DateTime date = DateTime.Parse(parts[1]);
                                    DateTime checkIn = DateTime.ParseExact(parts[2], "HH:mm", CultureInfo.InvariantCulture);
                                    DateTime checkOut = DateTime.ParseExact(parts[3], "HH:mm", CultureInfo.InvariantCulture);

                                    // Объединяем дату и время
                                    DateTime checkInDateTime = date.Date.Add(checkIn.TimeOfDay);
                                    DateTime checkOutDateTime = date.Date.Add(checkOut.TimeOfDay);

                                    // Рассчитываем часы работы
                                    int hoursWorked = (int)Math.Round((checkOutDateTime - checkInDateTime).TotalHours);

                                    // Проверяем, существует ли запись
                                    var existingRecord = await _context.AttendanceRecords
                                        .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == date.Date);

                                    if (existingRecord != null)
                                    {
                                        // Обновляем существующую запись
                                        existingRecord.CheckIn = checkInDateTime;
                                        existingRecord.CheckOut = checkOutDateTime;
                                        existingRecord.HoursWorked = hoursWorked;
                                        existingRecord.UpdatedAt = DateTime.UtcNow;
                                    }
                                    else
                                    {
                                        // Создаем новую запись
                                        var newRecord = new AttendanceRecord
                                        {
                                            EmployeeId = employeeId,
                                            Date = date.Date,
                                            CheckIn = checkInDateTime,
                                            CheckOut = checkOutDateTime,
                                            HoursWorked = hoursWorked,
                                            SourceSystem = "Import",
                                            CreatedAt = DateTime.UtcNow
                                        };

                                        await _context.AttendanceRecords.AddAsync(newRecord);
                                    }

                                    importedCount++;
                                }
                                catch
                                {
                                    errorCount++;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Настройка лицензии EPPlus
                        ExcelPackage.License.SetNonCommercialPersonal("Anon");
                        // Обрабатываем Excel
                        using (var package = new ExcelPackage(stream))
                        {
                            var worksheet = package.Workbook.Worksheets[0];
                            int rowCount = worksheet.Dimension.Rows;

                            for (int row = 2; row <= rowCount; row++) // Начинаем со второй строки (пропускаем заголовок)
                            {
                                try
                                {
                                    int employeeId = int.Parse(worksheet.Cells[row, 1].Value.ToString());
                                    DateTime date = DateTime.Parse(worksheet.Cells[row, 2].Value.ToString());

                                    // Получаем время
                                    DateTime checkIn, checkOut;

                                    // Пробуем разные форматы времени
                                    if (worksheet.Cells[row, 3].Value is DateTime)
                                    {
                                        checkIn = (DateTime)worksheet.Cells[row, 3].Value;
                                    }
                                    else
                                    {
                                        checkIn = DateTime.ParseExact(worksheet.Cells[row, 3].Value.ToString(), "HH:mm", CultureInfo.InvariantCulture);
                                    }

                                    if (worksheet.Cells[row, 4].Value is DateTime)
                                    {
                                        checkOut = (DateTime)worksheet.Cells[row, 4].Value;
                                    }
                                    else
                                    {
                                        checkOut = DateTime.ParseExact(worksheet.Cells[row, 4].Value.ToString(), "HH:mm", CultureInfo.InvariantCulture);
                                    }

                                    // Объединяем дату и время
                                    DateTime checkInDateTime = date.Date.Add(checkIn.TimeOfDay);
                                    DateTime checkOutDateTime = date.Date.Add(checkOut.TimeOfDay);

                                    // Рассчитываем часы работы
                                    int hoursWorked = (int)Math.Round((checkOutDateTime - checkInDateTime).TotalHours);

                                    // Проверяем, существует ли запись
                                    var existingRecord = await _context.AttendanceRecords
                                        .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == date.Date);

                                    if (existingRecord != null)
                                    {
                                        // Обновляем существующую запись
                                        existingRecord.CheckIn = checkInDateTime;
                                        existingRecord.CheckOut = checkOutDateTime;
                                        existingRecord.HoursWorked = hoursWorked;
                                        existingRecord.UpdatedAt = DateTime.UtcNow;
                                    }
                                    else
                                    {
                                        // Создаем новую запись
                                        var newRecord = new AttendanceRecord
                                        {
                                            EmployeeId = employeeId,
                                            Date = date.Date,
                                            CheckIn = checkInDateTime,
                                            CheckOut = checkOutDateTime,
                                            HoursWorked = hoursWorked,
                                            SourceSystem = "Import",
                                            CreatedAt = DateTime.UtcNow
                                        };

                                        await _context.AttendanceRecords.AddAsync(newRecord);
                                    }

                                    importedCount++;
                                }
                                catch
                                {
                                    errorCount++;
                                }
                            }
                        }
                    }

                    // Сохраняем изменения в базе данных
                    await _context.SaveChangesAsync();

                    StatusMessage = $"Импорт успешно завершен. Обработано записей: {importedCount}, с ошибками: {errorCount}.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при импорте данных: {ex.Message}";
            }

            return RedirectToPage();
        }

        private async Task LoadEmployeesAsync()
        {
            AllEmployees = (await _employeeService.GetAllEmployeesAsync()).ToList();
            EmployeeList = new SelectList(AllEmployees, "Id", "FullName");
        }

        private async Task PrepareCalendarDataAsync()
        {
            // Получаем данные о посещаемости для календаря
            var calendarData = await _context.AttendanceRecords
                .Include(a => a.Employee)
                .Where(a => a.Date >= StartDate && a.Date <= EndDate)
                .ToListAsync();

            // Формируем события календаря
            CalendarEvents = calendarData.Select(a => new CalendarEvent
            {
                Id = a.Id.ToString(),
                Title = $"{a.Employee.FullName}: {a.CheckIn.ToString("HH:mm")} - {a.CheckOut.ToString("HH:mm")}",
                Start = a.Date.ToString("yyyy-MM-dd"),
                ClassName = a.CheckIn.TimeOfDay > new TimeSpan(9, 0, 0) ? "bg-warning" : "bg-success"
            }).ToList();
        }

        private async Task PrepareChartDataAsync()
        {
            // Получаем данные для графиков по дням
            var dates = Enumerable.Range(0, (EndDate - StartDate).Days + 1)
                .Select(offset => StartDate.AddDays(offset).Date)
                .ToList();

            ChartDates = dates.Select(d => d.ToString("dd.MM")).ToList();

            // Получаем количество сотрудников по дням
            var dailyCounts = await _context.AttendanceRecords
                .Where(a => a.Date >= StartDate && a.Date <= EndDate)
                .GroupBy(a => a.Date.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.Date, v => v.Count);

            ChartCounts = dates.Select(d => dailyCounts.ContainsKey(d) ? dailyCounts[d] : 0).ToList();

            // Получаем среднее время прихода и ухода по дням
            var result = await _context.AttendanceRecords
     .Where(a => a.Date >= StartDate && a.Date <= EndDate)
     .Select(a => new { a.Date, a.CheckIn }) // забираем только нужные поля
     .ToListAsync(); // Загружаем данные в память

            var checkInTimes = result
                .GroupBy(a => a.Date.Date) // Группируем по дате (без времени)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(a => a.CheckIn.TimeOfDay.TotalHours) // Вычисляем среднее время
                );

            var result1 = await _context.AttendanceRecords
    .Where(a => a.Date >= StartDate && a.Date <= EndDate)
    .Select(a => new { a.Date, a.CheckOut }) // загружаем только нужные поля
    .ToListAsync(); // материализуем в памяти

            var checkOutTimes = result1
                .GroupBy(a => a.Date.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(a => a.CheckOut.TimeOfDay.TotalHours)
                );

            AvgCheckInTimes = dates.Select(d => checkInTimes.ContainsKey(d) ? checkInTimes[d] : 0).ToList();
            AvgCheckOutTimes = dates.Select(d => checkOutTimes.ContainsKey(d) ? checkOutTimes[d] : 0).ToList();
        }

        private async Task PrepareEmployeeStatsAsync()
        {
            // Получаем статистику по сотрудникам
            var employees = await _context.Employees.ToListAsync();
            var employeeStats = new List<EmployeeAttendanceStats>();

            foreach (var employee in employees)
            {
                var attendanceRecords = await _context.AttendanceRecords
                    .Where(a => a.EmployeeId == employee.Id && a.Date >= StartDate && a.Date <= EndDate)
                    .ToListAsync();

                if (attendanceRecords.Any())
                {
                    var stat = new EmployeeAttendanceStats
                    {
                        EmployeeId = employee.Id,
                        EmployeeName = employee.FullName,
                        AttendanceDays = attendanceRecords.Count,
                        TotalHours = attendanceRecords.Sum(a => a.HoursWorked),
                        LateCount = attendanceRecords.Count(a => a.CheckIn.TimeOfDay > new TimeSpan(9, 0, 0))
                    };

                    stat.PunctualityPercentage = stat.AttendanceDays > 0
                        ? Math.Round(100 - ((double)stat.LateCount / stat.AttendanceDays * 100), 1)
                        : 0;

                    employeeStats.Add(stat);
                }
            }

            EmployeeAttendanceStats = employeeStats;
        }
    }

    public class CalendarEvent
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Start { get; set; }
        public string ClassName { get; set; }
    }

    public class EmployeeAttendanceStats
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public int AttendanceDays { get; set; }
        public int TotalHours { get; set; }
        public int LateCount { get; set; }
        public double PunctualityPercentage { get; set; }
    }
}