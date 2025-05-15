using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Evaluations
{
    public class IndexModel : PageModel
    {
        private readonly IEvaluationService _evaluationService;
        private readonly IEmployeeService _employeeService;
        private readonly IUserService _userService;
        private readonly int _pageSize = 10;

        public IndexModel(
            IEvaluationService evaluationService,
            IEmployeeService employeeService,
            IUserService userService)
        {
            _evaluationService = evaluationService;
            _employeeService = employeeService;
            _userService = userService;
        }

        public List<EmployeeEvaluation> Evaluations { get; set; } = new List<EmployeeEvaluation>();
        public SelectList DepartmentList { get; set; }
        public SelectList EvaluatorList { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalEvaluations { get; set; }
        public double AverageScore { get; set; }
        public DateTime? LatestEvaluationDate { get; set; }
        public DepartmentChartData DepartmentChartData1 { get; set; } = new DepartmentChartData();

        [BindProperty(SupportsGet = true)]
        public int? DepartmentId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? EvaluatorId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Проверка аутентификации
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // Получаем текущего пользователя
            var currentUser = await _userService.GetCurrentUserAsync(User);
            if (currentUser == null)
                return RedirectToPage("/Account/Login");

            // Загружаем все оценки
            var allEvaluations = await _evaluationService.GetAllEvaluationsAsync();

            // Применяем фильтры
            var filteredEvaluations = ApplyFilters(allEvaluations);

            // Подсчитываем общую статистику
            CalculateStatistics(filteredEvaluations);

            // Разбиваем на страницы
            CurrentPage = Page < 1 ? 1 : Page;
            TotalEvaluations = filteredEvaluations.Count();
            TotalPages = (int)Math.Ceiling(TotalEvaluations / (double)_pageSize);

            // Получаем элементы для текущей страницы
            Evaluations = filteredEvaluations
                .Skip((CurrentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            // Подготавливаем данные для выпадающих списков
            await PrepareDropdowns();

            // Подготавливаем данные для графика
            PrepareChartData(filteredEvaluations);

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            // Проверка аутентификации
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            try
            {
                // Удаляем оценку
                var result = await _evaluationService.DeleteEvaluationAsync(id);

                if (result)
                {
                    StatusMessage = "Оценка успешно удалена.";
                }
                else
                {
                    StatusMessage = "Ошибка при удалении оценки.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }

            return RedirectToPage();
        }

        private IEnumerable<EmployeeEvaluation> ApplyFilters(IEnumerable<EmployeeEvaluation> evaluations)
        {
            var filtered = evaluations;

            // Фильтр по отделу
            if (DepartmentId.HasValue && DepartmentId.Value > 0)
            {
                filtered = filtered.Where(e => e.Employee.DepartmentId == DepartmentId.Value);
            }

            // Фильтр по оценивающему
            if (EvaluatorId.HasValue && EvaluatorId.Value > 0)
            {
                filtered = filtered.Where(e => e.EvaluatorId == EvaluatorId.Value);
            }

            // Фильтр по дате начала
            if (StartDate.HasValue)
            {
                filtered = filtered.Where(e => e.EvaluationDate >= StartDate.Value);
            }

            // Фильтр по дате окончания
            if (EndDate.HasValue)
            {
                filtered = filtered.Where(e => e.EvaluationDate <= EndDate.Value);
            }

            return filtered.OrderByDescending(e => e.EvaluationDate);
        }

        private void CalculateStatistics(IEnumerable<EmployeeEvaluation> evaluations)
        {
            if (evaluations.Any())
            {
                AverageScore = evaluations.Average(e => e.Score);
                LatestEvaluationDate = evaluations.Max(e => e.EvaluationDate);
            }
            else
            {
                AverageScore = 0;
                LatestEvaluationDate = null;
            }
        }

        private async Task PrepareDropdowns()
        {
            // Список отделов
            var departments = await _employeeService.GetAllDepartmentsAsync();
            DepartmentList = new SelectList(departments, "Id", "Name");

            // Список оценивающих (пользователей с ролью оценивающего)
            var evaluators = await _userService.GetUsersInRoleAsync("Evaluator");
            if (!evaluators.Any())
            {
                // Если нет пользователей с ролью "Evaluator", берем всех пользователей
                evaluators = await _userService.GetAllUsersAsync();
            }
            EvaluatorList = new SelectList(evaluators, "Id", "Username");
        }

        private void PrepareChartData(IEnumerable<EmployeeEvaluation> evaluations)
        {
            // Группируем оценки по отделам
            var departmentGroups = evaluations
                .GroupBy(e => e.Employee.Department.Name)
                .Select(g => new
                {
                    Department = g.Key,
                    Evaluations = g.ToList(),
                    AverageScore = g.Average(e => e.Score),
                    MaxScore = g.Max(e => e.Score),
                    MinScore = g.Min(e => e.Score)
                })
                .OrderByDescending(g => g.AverageScore)
                .ToList();

            // Формируем данные для графика
            DepartmentChartData1 = new DepartmentChartData
            {
                Labels = departmentGroups.Select(g => g.Department).ToList(),
                Averages = departmentGroups.Select(g => g.AverageScore).ToList(),
                Maximums = departmentGroups.Select(g => g.MaxScore).ToList(),
                Minimums = departmentGroups.Select(g => g.MinScore).ToList()
            };
        }

        public string GetPageUrl(int pageNumber)
        {
            return Url.Page("./Index", new
            {
                departmentId = DepartmentId,
                evaluatorId = EvaluatorId,
                startDate = StartDate?.ToString("yyyy-MM-dd"),
                endDate = EndDate?.ToString("yyyy-MM-dd"),
                page = pageNumber
            });
        }

        public class DepartmentChartData
        {
            public List<string> Labels { get; set; } = new List<string>();
            public List<double> Averages { get; set; } = new List<double>();
            public List<double> Maximums { get; set; } = new List<double>();
            public List<double> Minimums { get; set; } = new List<double>();
        }
    }
}