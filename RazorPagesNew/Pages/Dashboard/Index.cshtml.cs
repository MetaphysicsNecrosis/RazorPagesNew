using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesNew.Data;
using RazorPagesNew.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;

namespace RazorPagesNew.Pages.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly MyApplicationDbContext _context;
        private readonly IEmployeeService _employeeService;
        private readonly IEvaluationService _evaluationService;
        private readonly IUserService _userService;

        public IndexModel(
            MyApplicationDbContext context,
            IEmployeeService employeeService,
            IEvaluationService evaluationService,
            IUserService userService)
        {
            _context = context;
            _employeeService = employeeService;
            _evaluationService = evaluationService;
            _userService = userService;
        }

        public int TotalEmployees { get; set; }
        public double AverageScore { get; set; }
        public int TotalEvaluations { get; set; }
        public int CompletedTasks { get; set; }

        public List<TopEmployeeViewModel> TopEmployees { get; set; } = new();
        public List<DepartmentPerformanceViewModel> DepartmentPerformance { get; set; } = new();
        public List<EmployeeEvaluation> RecentEvaluations { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Проверка аутентификации
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // Получаем текущего пользователя
            var currentUser = await _userService.GetCurrentUserAsync(User);
            if (currentUser == null)
                return RedirectToPage("/Account/Login");

            // Вычисление статистики
            await CalculateStatistics();

            // Получение лучших сотрудников
            await GetTopEmployees();

            // Получение производительности по отделам
            await GetDepartmentPerformance();

            // Получение последних оценок
            await GetRecentEvaluations();

            return Page();
        }

        private async Task CalculateStatistics()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // Общее количество сотрудников
            TotalEmployees = await _context.Employees.CountAsync();

            // Средняя оценка всех сотрудников
            var evaluations = await _context.EmployeeEvaluations.ToListAsync();
            AverageScore = evaluations.Any() ? evaluations.Average(e => e.Score) : 0;

            // Количество оценок за текущий месяц
            TotalEvaluations = await _context.EmployeeEvaluations
                .Where(e => e.EvaluationDate >= startOfMonth && e.EvaluationDate <= today)
                .CountAsync();

            // Количество выполненных задач за текущий месяц
            CompletedTasks = await _context.TaskRecords
                .Where(t => t.CompletedAt >= startOfMonth && t.CompletedAt <= today)
                .CountAsync();
        }

        private async Task GetTopEmployees()
        {
            // Получаем данные о лучших сотрудниках по среднему значению оценок
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.EmployeeEvaluations)
                .Include(e => e.TaskRecords)
                .ToListAsync();

            TopEmployees = employees
                .Select(e => new TopEmployeeViewModel
                {
                    Id = e.Id,
                    FullName = e.FullName,
                    Department = e.Department,
                    Score = e.EmployeeEvaluations.Any() ? e.EmployeeEvaluations.Average(ev => ev.Score) : 0,
                    Efficiency = e.TaskRecords.Any(t => t.EfficiencyScore.HasValue)
                        ? e.TaskRecords.Where(t => t.EfficiencyScore.HasValue).Average(t => t.EfficiencyScore.Value) / 100
                        : 0
                })
                .OrderByDescending(e => e.Score)
                .Take(5)
                .ToList();
        }

        private async Task GetDepartmentPerformance()
        {
            // Получаем данные о производительности по отделам
            var departments = await _context.Departments
                .Include(d => d.Employees)
                .ThenInclude(e => e.EmployeeEvaluations)
                .ToListAsync();

            DepartmentPerformance = departments
                .Select(d => new DepartmentPerformanceViewModel
                {
                    Name = d.Name,
                    EmployeeCount = d.Employees.Count,
                    AverageScore = d.Employees.SelectMany(e => e.EmployeeEvaluations).Any()
                        ? d.Employees.SelectMany(e => e.EmployeeEvaluations).Average(e => e.Score)
                        : 0
                })
                .OrderByDescending(d => d.AverageScore)
                .ToList();
        }

        private async Task GetRecentEvaluations()
        {
            // Получаем последние оценки
            RecentEvaluations = await _context.EmployeeEvaluations
                .Include(e => e.Employee)
                .Include(e => e.Evaluator)
                .OrderByDescending(e => e.EvaluationDate)
                .Take(5)
                .ToListAsync();
        }
    }

    public class TopEmployeeViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public Department Department { get; set; }
        public double Score { get; set; }
        public double Efficiency { get; set; }
    }

    public class DepartmentPerformanceViewModel
    {
        public string Name { get; set; }
        public int EmployeeCount { get; set; }
        public double AverageScore { get; set; }
    }
}
