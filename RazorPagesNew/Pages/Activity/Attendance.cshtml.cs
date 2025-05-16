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

        // ������ ��� ���������
        public List<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();

        // ������ ��� ��������
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
            // �������� ��������������
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // ��������� ��������� ����������
            CurrentSort = sortOrder;
            EmployeeId = employeeId;
            CurrentPage = pageIndex ?? 1;

            // ������������� ���� �� ���������, ���� �� �������
            StartDate = startDate ?? DateTime.Today.AddDays(-30);
            EndDate = endDate ?? DateTime.Today;

            // ������������� ����������� ����������
            DateSort = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            NameSort = sortOrder == "name" ? "name_desc" : "name";

            // ���� ����� ��������� ������, ������������ �� ������ ��������
            if (searchTerm != null)
            {
                CurrentPage = 1;
            }
            else
            {
                searchTerm = currentFilter;
            }

            CurrentFilter = searchTerm;

            // ��������� ������ ����������� ��� ����������
            await LoadEmployeesAsync();

            // �������� ������ � ������������ �� ��������
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

            // ���������� �����������
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

            // �������� ����� ���������� �������
            var totalItems = await attendanceQuery.CountAsync();

            // ���������
            int pageSize = 10;
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            AttendanceRecords = await attendanceQuery
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ���������� ������ ��� ���������
            await PrepareCalendarDataAsync();

            // ���������� ������ ��� ��������
            await PrepareChartDataAsync();

            // ���������� ���������� �� �����������
            await PrepareEmployeeStatsAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync(int employeeId, DateTime date, string checkIn, string checkOut)
        {
            // �������� ��������������
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // ������� ���������
            if (employeeId <= 0 || date == default || string.IsNullOrEmpty(checkIn) || string.IsNullOrEmpty(checkOut))
            {
                StatusMessage = "������: ������������ ������. ����������, ��������� ��� ����.";
                return RedirectToPage();
            }

            try
            {
                // ������ �����
                var checkInTime = DateTime.Parse(date.ToShortDateString() + " " + checkIn);
                var checkOutTime = DateTime.Parse(date.ToShortDateString() + " " + checkOut);

                // ���������, ��� ����� ����� ����� ������� �������
                if (checkOutTime <= checkInTime)
                {
                    StatusMessage = "������: ����� ����� ������ ���� ����� ������� �������.";
                    return RedirectToPage();
                }

                // ������������ ���������� ������������ �����
                int hoursWorked = (int)Math.Round((checkOutTime - checkInTime).TotalHours);

                // ������� ����� ������
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

                // ���������, �� ���������� �� ��� ������ �� ��� ���� ��� ������� ����������
                var existingRecord = await _context.AttendanceRecords
                    .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == date.Date);

                if (existingRecord != null)
                {
                    StatusMessage = $"������: ������ � ������������ ��� ������� ���������� �� {date.ToShortDateString()} ��� ����������.";
                    return RedirectToPage();
                }

                // ��������� ������
                await _context.AttendanceRecords.AddAsync(attendanceRecord);
                await _context.SaveChangesAsync();

                StatusMessage = $"������ � ������������ �� {date.ToShortDateString()} ������� �������.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"������: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(int id, int employeeId, DateTime date, string checkIn, string checkOut)
        {
            // �������� ��������������
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // ������� ���������
            if (id <= 0 || employeeId <= 0 || date == default || string.IsNullOrEmpty(checkIn) || string.IsNullOrEmpty(checkOut))
            {
                StatusMessage = "������: ������������ ������. ����������, ��������� ��� ����.";
                return RedirectToPage();
            }

            try
            {
                // �������� ������������ ������
                var attendanceRecord = await _context.AttendanceRecords.FindAsync(id);
                if (attendanceRecord == null)
                {
                    StatusMessage = "������: ������ �� �������.";
                    return RedirectToPage();
                }

                // ������ �����
                var checkInTime = DateTime.Parse(date.ToShortDateString() + " " + checkIn);
                var checkOutTime = DateTime.Parse(date.ToShortDateString() + " " + checkOut);

                // ���������, ��� ����� ����� ����� ������� �������
                if (checkOutTime <= checkInTime)
                {
                    StatusMessage = "������: ����� ����� ������ ���� ����� ������� �������.";
                    return RedirectToPage();
                }

                // ������������ ���������� ������������ �����
                int hoursWorked = (int)Math.Round((checkOutTime - checkInTime).TotalHours);

                // ��������� ������
                attendanceRecord.Date = date.Date;
                attendanceRecord.CheckIn = checkInTime;
                attendanceRecord.CheckOut = checkOutTime;
                attendanceRecord.HoursWorked = hoursWorked;
                attendanceRecord.UpdatedAt = DateTime.UtcNow;

                _context.AttendanceRecords.Update(attendanceRecord);
                await _context.SaveChangesAsync();

                StatusMessage = $"������ � ������������ �� {date.ToShortDateString()} ������� ���������.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"������: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            // �������� ��������������
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // ������� ���������
            if (id <= 0)
            {
                StatusMessage = "������: ������������ ������������� ������.";
                return RedirectToPage();
            }

            try
            {
                // �������� ������������ ������
                var attendanceRecord = await _context.AttendanceRecords.FindAsync(id);
                if (attendanceRecord == null)
                {
                    StatusMessage = "������: ������ �� �������.";
                    return RedirectToPage();
                }

                // ������� ������
                _context.AttendanceRecords.Remove(attendanceRecord);
                await _context.SaveChangesAsync();

                StatusMessage = "������ � ������������ ������� �������.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"������: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            // �������� ��������������
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            try
            {
                // �������� ��� ������ � ������������
                var records = await _context.AttendanceRecords
                    .Include(a => a.Employee)
                    .OrderBy(a => a.Date)
                    .ThenBy(a => a.Employee.FullName)
                    .ToListAsync();

                // ��������� �������� EPPlus
                ExcelPackage.License.SetNonCommercialPersonal("Anon");
                // ������� ����� ����� Excel
                using (var package = new ExcelPackage())
                {
                    // ��������� ����
                    var worksheet = package.Workbook.Worksheets.Add("������������");

                    // ����� ���������
                    var headerStyle = worksheet.Cells["A1:G1"].Style;
                    headerStyle.Font.Bold = true;
                    headerStyle.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerStyle.Fill.BackgroundColor.SetColor(Color.LightGray);
                    headerStyle.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    // ��������� ���������
                    worksheet.Cells[1, 1].Value = "ID ����������";
                    worksheet.Cells[1, 2].Value = "��� ����������";
                    worksheet.Cells[1, 3].Value = "����";
                    worksheet.Cells[1, 4].Value = "����� �������";
                    worksheet.Cells[1, 5].Value = "����� �����";
                    worksheet.Cells[1, 6].Value = "���� ������";
                    worksheet.Cells[1, 7].Value = "���������";

                    // ��������� ������
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
                        worksheet.Cells[row, 7].Value = isLate ? "��" : "���";
                        if (isLate)
                        {
                            worksheet.Cells[row, 7].Style.Font.Color.SetColor(Color.Red);
                        }

                        row++;
                    }

                    // �������������� ��������� ������ ��������
                    worksheet.Cells.AutoFitColumns();

                    // ���������� ����
                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    string fileName = $"������������_{DateTime.Now.ToString("yyyy-MM-dd")}.xlsx";
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"������ ��� �������� ������: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostImportAsync(IFormFile importFile)
        {
            // �������� ��������������
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            if (importFile == null || importFile.Length == 0)
            {
                StatusMessage = "������: ���� �� ������ ��� ����.";
                return RedirectToPage();
            }

            try
            {
                // �������� �������� ������������
                var currentUser = await _userService.GetCurrentUserAsync(User);
                if (currentUser == null)
                    return RedirectToPage("/Account/Login");

                // ��������� ���������� �����
                var fileExtension = Path.GetExtension(importFile.FileName).ToLower();
                if (fileExtension != ".csv" && fileExtension != ".xlsx" && fileExtension != ".xls")
                {
                    StatusMessage = "������: �������������� ������ ����� CSV � Excel.";
                    return RedirectToPage();
                }

                int importedCount = 0;
                int errorCount = 0;

                // ��������� ����
                using (var stream = importFile.OpenReadStream())
                {
                    if (fileExtension == ".csv")
                    {
                        // ������������ CSV
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            string line;
                            bool isFirstLine = true;

                            while ((line = reader.ReadLine()) != null)
                            {
                                if (isFirstLine)
                                {
                                    isFirstLine = false;
                                    continue; // ���������� ���������
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

                                    // ���������� ���� � �����
                                    DateTime checkInDateTime = date.Date.Add(checkIn.TimeOfDay);
                                    DateTime checkOutDateTime = date.Date.Add(checkOut.TimeOfDay);

                                    // ������������ ���� ������
                                    int hoursWorked = (int)Math.Round((checkOutDateTime - checkInDateTime).TotalHours);

                                    // ���������, ���������� �� ������
                                    var existingRecord = await _context.AttendanceRecords
                                        .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == date.Date);

                                    if (existingRecord != null)
                                    {
                                        // ��������� ������������ ������
                                        existingRecord.CheckIn = checkInDateTime;
                                        existingRecord.CheckOut = checkOutDateTime;
                                        existingRecord.HoursWorked = hoursWorked;
                                        existingRecord.UpdatedAt = DateTime.UtcNow;
                                    }
                                    else
                                    {
                                        // ������� ����� ������
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
                        // ��������� �������� EPPlus
                        ExcelPackage.License.SetNonCommercialPersonal("Anon");
                        // ������������ Excel
                        using (var package = new ExcelPackage(stream))
                        {
                            var worksheet = package.Workbook.Worksheets[0];
                            int rowCount = worksheet.Dimension.Rows;

                            for (int row = 2; row <= rowCount; row++) // �������� �� ������ ������ (���������� ���������)
                            {
                                try
                                {
                                    int employeeId = int.Parse(worksheet.Cells[row, 1].Value.ToString());
                                    DateTime date = DateTime.Parse(worksheet.Cells[row, 2].Value.ToString());

                                    // �������� �����
                                    DateTime checkIn, checkOut;

                                    // ������� ������ ������� �������
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

                                    // ���������� ���� � �����
                                    DateTime checkInDateTime = date.Date.Add(checkIn.TimeOfDay);
                                    DateTime checkOutDateTime = date.Date.Add(checkOut.TimeOfDay);

                                    // ������������ ���� ������
                                    int hoursWorked = (int)Math.Round((checkOutDateTime - checkInDateTime).TotalHours);

                                    // ���������, ���������� �� ������
                                    var existingRecord = await _context.AttendanceRecords
                                        .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == date.Date);

                                    if (existingRecord != null)
                                    {
                                        // ��������� ������������ ������
                                        existingRecord.CheckIn = checkInDateTime;
                                        existingRecord.CheckOut = checkOutDateTime;
                                        existingRecord.HoursWorked = hoursWorked;
                                        existingRecord.UpdatedAt = DateTime.UtcNow;
                                    }
                                    else
                                    {
                                        // ������� ����� ������
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

                    // ��������� ��������� � ���� ������
                    await _context.SaveChangesAsync();

                    StatusMessage = $"������ ������� ��������. ���������� �������: {importedCount}, � ��������: {errorCount}.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"������ ��� ������� ������: {ex.Message}";
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
            // �������� ������ � ������������ ��� ���������
            var calendarData = await _context.AttendanceRecords
                .Include(a => a.Employee)
                .Where(a => a.Date >= StartDate && a.Date <= EndDate)
                .ToListAsync();

            // ��������� ������� ���������
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
            // �������� ������ ��� �������� �� ����
            var dates = Enumerable.Range(0, (EndDate - StartDate).Days + 1)
                .Select(offset => StartDate.AddDays(offset).Date)
                .ToList();

            ChartDates = dates.Select(d => d.ToString("dd.MM")).ToList();

            // �������� ���������� ����������� �� ����
            var dailyCounts = await _context.AttendanceRecords
                .Where(a => a.Date >= StartDate && a.Date <= EndDate)
                .GroupBy(a => a.Date.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.Date, v => v.Count);

            ChartCounts = dates.Select(d => dailyCounts.ContainsKey(d) ? dailyCounts[d] : 0).ToList();

            // �������� ������� ����� ������� � ����� �� ����
            var result = await _context.AttendanceRecords
     .Where(a => a.Date >= StartDate && a.Date <= EndDate)
     .Select(a => new { a.Date, a.CheckIn }) // �������� ������ ������ ����
     .ToListAsync(); // ��������� ������ � ������

            var checkInTimes = result
                .GroupBy(a => a.Date.Date) // ���������� �� ���� (��� �������)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(a => a.CheckIn.TimeOfDay.TotalHours) // ��������� ������� �����
                );

            var result1 = await _context.AttendanceRecords
    .Where(a => a.Date >= StartDate && a.Date <= EndDate)
    .Select(a => new { a.Date, a.CheckOut }) // ��������� ������ ������ ����
    .ToListAsync(); // ������������� � ������

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
            // �������� ���������� �� �����������
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