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
            // �������� ��������������
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // �������� �������� ������������
            var currentUser = await _userService.GetCurrentUserAsync(User);
            if (currentUser == null)
                return RedirectToPage("/Account/Login");

            // ��������� ���������� ������ �����������
            await PopulateEmployeeList();

            // ������������� �������� �� ���������
            Evaluation = new EmployeeEvaluation
            {
                EvaluationDate = DateTime.UtcNow,
                EvaluatorId = currentUser.Id,
                OwnerId = currentUser.Id,
                Score = 0
            };

            // ���� ������� ID ����������, ������������� �����
            if (employeeId.HasValue)
            {
                var employee = await _employeeService.GetEmployeeByIdAsync(employeeId.Value);
                if (employee != null)
                {
                    Evaluation.EmployeeId = employee.Id;
                }
            }

            // ������������� ������ ������ - ��������� �����
            var today = DateTime.UtcNow;
            Summary = new WorkActivitySummary
            {
                PeriodStart = new DateTime(today.Year, today.Month, 1).AddMonths(-1),
                PeriodEnd = new DateTime(today.Year, today.Month, 1).AddDays(-1),
                OwnerId = currentUser.Id
            };

            // ��������� �������� ������
            await LoadEvaluationCriteria();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // �������� ��������������
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // �������� �������� ������������
            var currentUser = await _userService.GetCurrentUserAsync(User);
            if (currentUser == null)
                return RedirectToPage("/Account/Login");

            // ��������� ���������� ������
           /* if (!ModelState.IsValid)
            {
                await PopulateEmployeeList();
                await LoadEvaluationCriteria();
                return Page();
            }*/

            try
            {
                var userCurr = await _userService.GetCurrentUserAsync(User);
                // ���������� ������ ���������� �� ��������� ������
                Summary.EmployeeId = Evaluation.EmployeeId;
                var generatedSummary = await _evaluationService.GenerateWorkActivitySummaryAsync(
                    Evaluation.EmployeeId,
                    Summary.PeriodStart,
                    Summary.PeriodEnd,
                    userCurr.Id
                    
                    );

                // ��������� ������
                var savedSummary = await _evaluationService.CreateWorkActivitySummaryAsync(generatedSummary);

                // ��������� ������ �� �������
                Evaluation.SummaryId = savedSummary.Id;

                Evaluation.EvaluatorId = userCurr.Id;
                Evaluation.OwnerId = userCurr.Id;
                // ��������� ������
                var savedEvaluation = await _evaluationService.CreateEvaluationAsync(Evaluation);

                // ��������� ������ �� ���������
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
                ModelState.AddModelError(string.Empty, $"��������� ������: {ex.Message}");
                await PopulateEmployeeList();
                await LoadEvaluationCriteria();
                return Page();
            }
        }

        private async Task PopulateEmployeeList()
        {
            var employees = await _employeeService.GetAllEmployeesAsync();
            EmployeeList = new SelectList(
                employees.Where(e => e.DismissalDate == null), // ������ �������� ����������
                "Id",
                "FullName"
            );
        }

        private async Task LoadEvaluationCriteria()
        {
            var criteria = await _evaluationService.GetAllCriteriaAsync();

            // ������� ������ ��������� � ���������� ��������
            CriteriaScores = criteria.Select(c => new CriteriaScoreViewModel
            {
                CriterionId = c.Id,
                CriterionName = c.Name,
                Weight = c.Weight,
                Score = 50 // ��������� �������� ���������� �����
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