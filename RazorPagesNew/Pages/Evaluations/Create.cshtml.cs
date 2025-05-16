using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Evaluations
{
    public class CreateModel : PageModel
    {
        private readonly IEvaluationService _evaluationService;
        private readonly IEmployeeService _employeeService;
        private readonly IUserService _userService;

        public CreateModel(
            IEvaluationService evaluationService,
            IEmployeeService employeeService,
            IUserService userService)
        {
            _evaluationService = evaluationService;
            _employeeService = employeeService;
            _userService = userService;
        }

        [BindProperty]
        public EmployeeEvaluation Evaluation { get; set; } = new EmployeeEvaluation();

        [BindProperty]
        public WorkActivitySummary Summary { get; set; } = new WorkActivitySummary();

        [BindProperty]
        public List<CriteriaScoreViewModel> CriteriaScores { get; set; } = new List<CriteriaScoreViewModel>();

        public SelectList EmployeeList { get; set; }

        public async Task<IActionResult> OnGetAsync(int? employeeId = null)
        {
            // Проверка аутентификации
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // Получаем текущего пользователя
            var currentUser = await _userService.GetCurrentUserAsync(User);
            if (currentUser == null)
                return RedirectToPage("/Account/Login");

            // Заполняем выпадающий список сотрудников
            await PopulateEmployeeList();

            // Устанавливаем значения по умолчанию
            Evaluation = new EmployeeEvaluation
            {
                EvaluationDate = DateTime.UtcNow,
                EvaluatorId = currentUser.Id,
                OwnerId = currentUser.Id,
                Score = 0
            };

            // Если передан ID сотрудника, предзаполняем форму
            if (employeeId.HasValue)
            {
                var employee = await _employeeService.GetEmployeeByIdAsync(employeeId.Value);
                if (employee != null)
                {
                    Evaluation.EmployeeId = employee.Id;
                }
            }

            // Устанавливаем период оценки - последний месяц
            var today = DateTime.UtcNow;
            Summary = new WorkActivitySummary
            {
                PeriodStart = new DateTime(today.Year, today.Month, 1).AddMonths(-1),
                PeriodEnd = new DateTime(today.Year, today.Month, 1).AddDays(-1),
                OwnerId = currentUser.Id
            };

            // Загружаем критерии оценки
            await LoadEvaluationCriteria();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Проверка аутентификации
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // Получаем текущего пользователя
            var currentUser = await _userService.GetCurrentUserAsync(User);
            if (currentUser == null)
                return RedirectToPage("/Account/Login");

            // Проверяем валидность модели
           /* if (!ModelState.IsValid)
            {
                await PopulateEmployeeList();
                await LoadEvaluationCriteria();
                return Page();
            }*/

            try
            {
                var userCurr = await _userService.GetCurrentUserAsync(User);
                // Генерируем сводку активности за указанный период
                Summary.EmployeeId = Evaluation.EmployeeId;
                var generatedSummary = await _evaluationService.GenerateWorkActivitySummaryAsync(
                    Evaluation.EmployeeId,
                    Summary.PeriodStart,
                    Summary.PeriodEnd,
                    userCurr.Id
                    
                    );

                // Сохраняем сводку
                var savedSummary = await _evaluationService.CreateWorkActivitySummaryAsync(generatedSummary);

                // Связываем оценку со сводкой
                Evaluation.SummaryId = savedSummary.Id;

                Evaluation.EvaluatorId = userCurr.Id;
                Evaluation.OwnerId = userCurr.Id;
                // Сохраняем оценку
                var savedEvaluation = await _evaluationService.CreateEvaluationAsync(Evaluation);

                // Сохраняем оценки по критериям
                foreach (var criteriaScore in CriteriaScores)
                {
                    var score = new EvaluationScore
                    {
                        EvaluationId = savedEvaluation.Id,
                        CriterionId = criteriaScore.CriterionId,
                        Score = criteriaScore.Score
                  
                    };

                    await _evaluationService.CreateScoreAsync(score);
                }

                return RedirectToPage("./Details", new { id = savedEvaluation.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Произошла ошибка: {ex.Message}");
                await PopulateEmployeeList();
                await LoadEvaluationCriteria();
                return Page();
            }
        }

        private async Task PopulateEmployeeList()
        {
            var employees = await _employeeService.GetAllEmployeesAsync();
            EmployeeList = new SelectList(
                employees.Where(e => e.DismissalDate == null), // Только активные сотрудники
                "Id",
                "FullName"
            );
        }

        private async Task LoadEvaluationCriteria()
        {
            var criteria = await _evaluationService.GetAllCriteriaAsync();

            // Создаем список критериев с начальными оценками
            CriteriaScores = criteria.Select(c => new CriteriaScoreViewModel
            {
                CriterionId = c.Id,
                CriterionName = c.Name,
                Weight = c.Weight,
                Score = 50 // Начальное значение посередине шкалы
            }).ToList();
        }

        public class CriteriaScoreViewModel
        {
            public int CriterionId { get; set; }
            public string CriterionName { get; set; }
            public double Weight { get; set; }
            public double Score { get; set; }
        }
    }
}