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
        private readonly IEmployeeService _employeeService;
        private readonly IEvaluationService _evaluationService;
        private readonly IUserService _userService;

        public CreateModel(
            IEmployeeService employeeService,
            IEvaluationService evaluationService,
            IUserService userService)
        {
            _employeeService = employeeService;
            _evaluationService = evaluationService;
            _userService = userService;
        }

        [BindProperty]
        public EmployeeEvaluation Evaluation { get; set; }

        [BindProperty]
        public WorkActivitySummary Summary { get; set; }

        [BindProperty]
        public List<CriteriaScoreViewModel> CriteriaScores { get; set; }

        [BindProperty]
        public string EvaluationModel { get; set; } = "weighted";

        // BSC �������������� ����
        [BindProperty]
        public double BSC_Financial_1 { get; set; }
        [BindProperty]
        public double BSC_Financial_2 { get; set; }
        [BindProperty]
        public double BSC_Financial_3 { get; set; }
        [BindProperty]
        public double BSC_Customer_1 { get; set; }
        [BindProperty]
        public double BSC_Customer_2 { get; set; }
        [BindProperty]
        public double BSC_Customer_3 { get; set; }
        [BindProperty]
        public double BSC_Process_1 { get; set; }
        [BindProperty]
        public double BSC_Process_2 { get; set; }
        [BindProperty]
        public double BSC_Process_3 { get; set; }
        [BindProperty]
        public double BSC_Learning_1 { get; set; }
        [BindProperty]
        public double BSC_Learning_2 { get; set; }
        [BindProperty]
        public double BSC_Learning_3 { get; set; }

        // KPI �������������� ����
        [BindProperty]
        public List<KpiMetricViewModel> KpiMetrics { get; set; }

        // MBO �������������� ����
        [BindProperty]
        public List<MboGoalViewModel> MboGoals { get; set; }

        public SelectList EmployeeList { get; set; }

        public async Task<IActionResult> OnGetAsync(int? employeeId)
        {
            await LoadEmployeeListAsync();
            await LoadCriteriaAsync();

            Evaluation = new EmployeeEvaluation
            {
                EvaluationDate = System.DateTime.UtcNow
            };

            Summary = new WorkActivitySummary
            {
                PeriodStart = System.DateTime.UtcNow.AddMonths(-1),
                PeriodEnd = System.DateTime.UtcNow
            };

            if (employeeId.HasValue)
            {
                Evaluation.EmployeeId = employeeId.Value;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
           /* if (!ModelState.IsValid)
            {
                await LoadEmployeeListAsync();
                await LoadCriteriaAsync();
                return Page();
            }*/

            // �������� �������� ������������
            var currentUser = await _userService.GetCurrentUserAsync(User);
            if (currentUser == null)
            {
                ModelState.AddModelError(string.Empty, "���������� ���������������� �������� ������������");
                await LoadEmployeeListAsync();
                await LoadCriteriaAsync();
                return Page();
            }

            // ������� ������ ���������� ����������
            var summary = await _evaluationService.CreateWorkActivitySummaryAsync(await _evaluationService.GenerateWorkActivitySummaryAsync(
                Evaluation.EmployeeId,
                Summary.PeriodStart,
                Summary.PeriodEnd, currentUser.Id));

            
            // ������������� ���� ��� ������
            Evaluation.EvaluatorId = currentUser.Id;
            Evaluation.SummaryId = summary.Id;
            Evaluation.OwnerId = currentUser.Id;

            // ������� ������
            var createdEvaluation = await _evaluationService.CreateEvaluationAsync(Evaluation);

            // ������� ������ �� ��������� � ����������� �� ������ ������
            if (EvaluationModel == "weighted")
            {
                await CreateStandardScoresAsync(createdEvaluation.Id);
            }
            else
            {
                // ��� ������ ������� ���������� ������ ����� ����
                // �������������� ���������� ��������� � �����������
                string modelInfo = "";

                switch (EvaluationModel)
                {
                    case "balanced":
                        modelInfo = "���������������� ������� ����������� (BSC)";
                        break;
                    case "mbo":
                        modelInfo = "���������� �� ����� (MBO)";
                        break;
                    case "kpi":
                        modelInfo = "�������� ���������� ������������� (KPI)";
                        break;
                }

                // ��������� ���������� � ������ � ����������
                if (!string.IsNullOrEmpty(modelInfo))
                {
                    Evaluation.Notes = $"[{modelInfo}] {Evaluation.Notes}";

                    // ��������� ������ � �����������
                    await _evaluationService.UpdateEvaluationAsync(Evaluation);
                }
            }

            return RedirectToPage("./Details", new { id = createdEvaluation.Id });
        }

        private async Task CreateStandardScoresAsync(int evaluationId)
        {
            foreach (var criteriaScore in CriteriaScores)
            {
                await _evaluationService.CreateScoreAsync(new EvaluationScore
                {
                    EvaluationId = evaluationId,
                    CriterionId = criteriaScore.CriterionId,
                    Score = criteriaScore.Score
                });
            }
        }

        private async Task LoadCriteriaAsync()
        {
            CriteriaScores = new List<CriteriaScoreViewModel>();

            var criteria = await _evaluationService.GetAllCriteriaAsync();
            foreach (var criterion in criteria)
            {
                CriteriaScores.Add(new CriteriaScoreViewModel
                {
                    CriterionId = criterion.Id,
                    CriterionName = criterion.Name,
                    Weight = criterion.Weight,
                    Score = 0
                });
            }
        }

        private async Task LoadEmployeeListAsync()
        {
            var employees = await _employeeService.GetAllEmployeesAsync();
            EmployeeList = new SelectList(employees, "Id", "FullName");
        }

        // ������ ��� ������������� ������ �� ��������

        public class CriteriaScoreViewModel
        {
            public int CriterionId { get; set; }
            public string CriterionName { get; set; }
            public double Weight { get; set; }
            public double Score { get; set; }
        }

        public class KpiMetricViewModel
        {
            public string Name { get; set; }
            public string Category { get; set; }
            public double Value { get; set; }
            public double Weight { get; set; }
            public bool IsInverse { get; set; }
        }

        public class MboGoalViewModel
        {
            public string Goal { get; set; }
            public double Weight { get; set; }
            public double Achievement { get; set; }
        }
    }
}