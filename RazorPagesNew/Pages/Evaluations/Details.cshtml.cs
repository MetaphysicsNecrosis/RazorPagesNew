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
            // Проверка аутентификации
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // Получаем текущего пользователя
            var currentUser = await _userService.GetCurrentUserAsync(User);
            if (currentUser == null)
                return RedirectToPage("/Account/Login");

            // Получаем данные об оценке
            Evaluation = await _evaluationService.GetEvaluationByIdAsync(id);

            if (Evaluation == null)
            {
                return NotFound();
            }

            // Получаем детальные оценки по критериям
            EvaluationScores = (await _evaluationService.GetScoresByEvaluationIdAsync(id)).ToList();

            return Page();
        }

        public string GetScoreDescription(double score)
        {
            if (score >= 90) return "Превосходно";
            if (score >= 80) return "Отлично";
            if (score >= 70) return "Хорошо";
            if (score >= 60) return "Выше среднего";
            if (score >= 50) return "Средне";
            if (score >= 40) return "Ниже среднего";
            if (score >= 30) return "Слабо";
            return "Неудовлетворительно";
        }
    }
}