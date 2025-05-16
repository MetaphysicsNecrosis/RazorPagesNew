using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Evaluations
{
    public class IndexModel : PageModel
    {
        private readonly MyApplicationDbContext _context;

        public IndexModel(MyApplicationDbContext context)
        {
            _context = context;
        }

        public class EvaluationListItem
        {
            public int Id { get; set; }
            public int EmployeeId { get; set; }
            public string EmployeeFullName { get; set; }
            public string DepartmentName { get; set; }
            public DateTime EvaluationDate { get; set; }
            public double Score { get; set; }
            public string EvaluatorUsername { get; set; }
        }

        public IList<EvaluationListItem> Evaluations { get; set; }
        public int TotalEvaluations { get; set; }
        public double AverageScore { get; set; }
        public DateTime? LatestEvaluationDate { get; set; }
        public SelectList DepartmentList { get; set; }
        public SelectList EvaluatorList { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? DepartmentId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? EvaluatorId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }

        // Данные для графика по отделам
        public Dictionary<string, object> DepartmentChartData { get; set; }

        public async Task OnGetAsync()
        {
            // Получаем списки для фильтров без навигационных свойств
            var departments = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new { d.Id, d.Name })
                .ToListAsync();

            DepartmentList = new SelectList(departments, "Id", "Name");

            var evaluators = await _context.Users
                .OrderBy(u => u.Username)
                .Select(u => new { u.Id, Name = u.Username })
                .ToListAsync();

            EvaluatorList = new SelectList(evaluators, "Id", "Name");

            // Подготовка запроса с учетом фильтров, но без навигационных свойств
            var query = _context.EmployeeEvaluations.AsQueryable();

            // Применяем фильтры
            if (DepartmentId.HasValue)
            {
                // Здесь нам нужно сначала получить ID сотрудников выбранного отдела
                var employeeIds = await _context.Employees
                    .Where(e => e.DepartmentId == DepartmentId.Value)
                    .Select(e => e.Id)
                    .ToListAsync();

                query = query.Where(e => employeeIds.Contains(e.EmployeeId));
            }

            if (EvaluatorId.HasValue)
            {
                query = query.Where(e => e.EvaluatorId == EvaluatorId.Value);
            }

            if (StartDate.HasValue)
            {
                query = query.Where(e => e.EvaluationDate >= StartDate.Value);
            }

            if (EndDate.HasValue)
            {
                query = query.Where(e => e.EvaluationDate <= EndDate.Value);
            }

            // Получаем общее количество оценок и вычисляем пагинацию
            TotalEvaluations = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalEvaluations / (double)PageSize);

            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            // Рассчитываем среднюю оценку и последнюю дату оценки
            if (TotalEvaluations > 0)
            {
                AverageScore = await query.AverageAsync(e => e.Score);
                LatestEvaluationDate = await query.MaxAsync(e => e.EvaluationDate);
            }

            // Загружаем данные оценок с пагинацией
            var evaluationIds = await query
                .OrderByDescending(e => e.EvaluationDate)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(e => e.Id)
                .ToListAsync();

            // Для каждой оценки загружаем необходимые данные без навигационных свойств
            var evaluationItems = new List<EvaluationListItem>();

            foreach (var id in evaluationIds)
            {
                var evaluation = await _context.EmployeeEvaluations
                    .Where(e => e.Id == id)
                    .Select(e => new
                    {
                        e.Id,
                        e.EmployeeId,
                        e.EvaluationDate,
                        e.Score,
                        e.EvaluatorId
                    })
                    .FirstOrDefaultAsync();

                if (evaluation != null)
                {
                    // Загружаем данные о сотруднике
                    var employee = await _context.Employees
                        .Where(e => e.Id == evaluation.EmployeeId)
                        .Select(e => new { e.FullName, e.DepartmentId })
                        .FirstOrDefaultAsync();

                    // Загружаем данные об отделе
                    string departmentName = "";
                    if (employee != null)
                    {
                        departmentName = await _context.Departments
                            .Where(d => d.Id == employee.DepartmentId)
                            .Select(d => d.Name)
                            .FirstOrDefaultAsync() ?? "";
                    }

                    // Загружаем данные об оценивающем
                    var evaluatorUsername = await _context.Users
                        .Where(u => u.Id == evaluation.EvaluatorId)
                        .Select(u => u.Username)
                        .FirstOrDefaultAsync() ?? "";

                    // Создаем объект для отображения
                    evaluationItems.Add(new EvaluationListItem
                    {
                        Id = evaluation.Id,
                        EmployeeId = evaluation.EmployeeId,
                        EmployeeFullName = employee?.FullName ?? "Неизвестно",
                        DepartmentName = departmentName,
                        EvaluationDate = evaluation.EvaluationDate,
                        Score = evaluation.Score,
                        EvaluatorUsername = evaluatorUsername
                    });
                }
            }

            Evaluations = evaluationItems;

            // Подготовка данных для графика по отделам
            await PrepareChartDataAsync();
        }

        private async Task PrepareChartDataAsync()
        {
            // Получаем статистику по отделам без навигационных свойств
            var departments = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new { d.Id, d.Name })
                .ToListAsync();

            var departmentLabels = new List<string>();
            var departmentAverages = new List<double>();
            var departmentMaximums = new List<double>();
            var departmentMinimums = new List<double>();

            foreach (var dept in departments)
            {
                // Получаем ID сотрудников этого отдела
                var employeeIds = await _context.Employees
                    .Where(e => e.DepartmentId == dept.Id)
                    .Select(e => e.Id)
                    .ToListAsync();

                // Получаем статистику оценок для этих сотрудников
                var evaluations = await _context.EmployeeEvaluations
                    .Where(e => employeeIds.Contains(e.EmployeeId))
                    .Select(e => e.Score)
                    .ToListAsync();

                if (evaluations.Any())
                {
                    departmentLabels.Add(dept.Name);
                    departmentAverages.Add(Math.Round(evaluations.Average(), 1));
                    departmentMaximums.Add(Math.Round(evaluations.Max(), 1));
                    departmentMinimums.Add(Math.Round(evaluations.Min(), 1));
                }
            }

            DepartmentChartData = new Dictionary<string, object>
            {
                { "labels", departmentLabels },
                { "averages", departmentAverages },
                { "maximums", departmentMaximums },
                { "minimums", departmentMinimums }
            };
        }

        public string GetPageUrl(int pageIndex)
        {
            var pageParams = new Dictionary<string, string>
            {
                { "pageIndex", pageIndex.ToString() }
            };

            if (DepartmentId.HasValue)
            {
                pageParams.Add("departmentId", DepartmentId.Value.ToString());
            }

            if (EvaluatorId.HasValue)
            {
                pageParams.Add("evaluatorId", EvaluatorId.Value.ToString());
            }

            if (StartDate.HasValue)
            {
                pageParams.Add("startDate", StartDate.Value.ToString("yyyy-MM-dd"));
            }

            if (EndDate.HasValue)
            {
                pageParams.Add("endDate", EndDate.Value.ToString("yyyy-MM-dd"));
            }

            // Формируем строку запроса
            return Url.Page("./Index", pageParams);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var evaluation = await _context.EmployeeEvaluations.FindAsync(id);

            if (evaluation != null)
            {
                _context.EmployeeEvaluations.Remove(evaluation);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}