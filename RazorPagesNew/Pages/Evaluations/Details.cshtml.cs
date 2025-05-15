using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Evaluations
{
    public class DetailsModel : PageModel
    {
        private readonly IEvaluationService _evaluationService;
        private readonly IUserService _userService;

        public DetailsModel(IEvaluationService evaluationService, IUserService userService)
        {
            _evaluationService = evaluationService;
            _userService = userService;
        }

        public EmployeeEvaluation Evaluation { get; set; }
        public List<EvaluationScore> EvaluationScores { get; set; } = new List<EvaluationScore>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // �������� ��������������
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // �������� �������� ������������
            var currentUser = await _userService.GetCurrentUserAsync(User);
            if (currentUser == null)
                return RedirectToPage("/Account/Login");

            // �������� ������ �� ������
            Evaluation = await _evaluationService.GetEvaluationByIdAsync(id);

            if (Evaluation == null)
            {
                return NotFound();
            }

            // �������� ��������� ������ �� ���������
            EvaluationScores = (await _evaluationService.GetScoresByEvaluationIdAsync(id)).ToList();

            return Page();
        }

        public string GetScoreDescription(double score)
        {
            if (score >= 90) return "�����������";
            if (score >= 80) return "�������";
            if (score >= 70) return "������";
            if (score >= 60) return "���� ��������";
            if (score >= 50) return "������";
            if (score >= 40) return "���� ��������";
            if (score >= 30) return "�����";
            return "�������������������";
        }
    }
}