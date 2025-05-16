using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Xunit.Sdk;

namespace RazorPagesNew.Pages.Criteria
{
    public class CreateModel : PageModel
    {
        private readonly MyApplicationDbContext _context;
        private readonly IEvaluationService _evaluationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserService _userService;

        public CreateModel(
            MyApplicationDbContext context,
            IEvaluationService evaluationService,
            IAuditLogService auditLogService,
            IUserService userService)
        {
            _context = context;
            _evaluationService = evaluationService;
            _auditLogService = auditLogService;
            _userService = userService;
        }

        [BindProperty]
        public CriterionCreateViewModel CriterionViewModel { get; set; }

        // �������� ��� ��������� �� ������� � �������� ���������
        [TempData]
        public string StatusMessage { get; set; }

        // ������ ������������ ��������� ��� ���������������� ���������
        public List<EvaluationCriterion> ExistingCriteria { get; set; }

        // ����� ��� ������������ ���������
        public double TotalExistingWeight { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // ��������� ����� ������� - ������ �������������� � HR ����� ��������� ��������
            if (!User.IsInRole("Admin") && !User.IsInRole("HR"))
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            // ��������� ������ ������������ ��������� ��� �����������
            ExistingCriteria = (await _evaluationService.GetAllCriteriaAsync()).ToList();

            // ��������� ����� ��� ������������ ���������
            TotalExistingWeight = ExistingCriteria.Sum(c => c.Weight);

            // �������������� ������ �������������
            CriterionViewModel = new CriterionCreateViewModel
            {
                // ������������� ������������� ��� ������ ��������
                Weight = Math.Round(GetRecommendedWeight(), 2)
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // ��������� ����� �������
            if (!User.IsInRole("Admin") && !User.IsInRole("HR"))
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            // ��������� ���������� ������
            if (!ModelState.IsValid)
            {
                // ������������� ������ ��� ��������
                ExistingCriteria = (await _evaluationService.GetAllCriteriaAsync()).ToList();
                TotalExistingWeight = ExistingCriteria.Sum(c => c.Weight);
                return Page();
            }

            // �������� �� ������������ �������� ��������
            var existingCriterion = _context.EvaluationCriteria.FirstOrDefault(c => c.Name == CriterionViewModel.Name);
            if (existingCriterion != null)
            {
                ModelState.AddModelError("CriterionViewModel.Name", "�������� � ����� ��������� ��� ����������.");
                ExistingCriteria = (await _evaluationService.GetAllCriteriaAsync()).ToList();
                TotalExistingWeight = ExistingCriteria.Sum(c => c.Weight);
                return Page();
            }

            // �������� �������� ������������
            var currentUser = await _userService.GetCurrentUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // ����������� ������ ������������� � �������� ������
            var criterion = new EvaluationCriterion
            {
                Name = CriterionViewModel.Name,
                Weight = CriterionViewModel.Weight,
             /*   Description = CriterionViewModel.Description,*/
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                // ��������� ����� ��������
                await _evaluationService.CreateCriterionAsync(criterion);

                // �������� ��������
                await _auditLogService.LogActivityAsync(
                    currentUser.Username,
                    Models.Enums.ActionType.Create,
                    "EvaluationCriterion",
                    criterion.Id.ToString(),
                    $"������ ����� �������� ������: {criterion.Name} � �����: {criterion.Weight:P0}"
                );

                // ������������� ��������� �� �������� ��������
                StatusMessage = $"�������� '{criterion.Name}' ������� ������.";

                // �������������� �� �������� �� ������� ���������
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                // ������������ ������
                StatusMessage = $"������: �� ������� ������� ��������. {ex.Message}";

                // ������������� ������ ��� ��������
                ExistingCriteria = (await _evaluationService.GetAllCriteriaAsync()).ToList();
                TotalExistingWeight = ExistingCriteria.Sum(c => c.Weight);

                return Page();
            }
        }

        // ��������� ������������� ��� ��� ������ ��������
        private double GetRecommendedWeight()
        {
            if (ExistingCriteria == null || !ExistingCriteria.Any())
            {
                return 1.0; // ���� ��� ���������, �� ��� 100%
            }

            double currentTotalWeight = ExistingCriteria.Sum(c => c.Weight);

            if (currentTotalWeight >= 1.0)
            {
                // ���� ����� ��� ��� 100% ��� ������, ���������� ��������� ���� ������������ ���������
                return 0.1; // ���������� ��� 10%
            }

            // ����� ���������� ��������� ���������� ���
            return Math.Min(0.3, 1.0 - currentTotalWeight); // �� ����� 30%
        }
    }

    // ������ ������������� ��� �������� ��������
    public class CriterionCreateViewModel
    {
        [Required(ErrorMessage = "�������� �������� �����������")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "�������� ������ ��������� �� 3 �� 100 ��������")]
        [Display(Name = "�������� ��������")]
        public string Name { get; set; }

        [Required(ErrorMessage = "��� �������� ����������")]
        [Range(0.01, 1.0, ErrorMessage = "��� ������ ���� �� 1% �� 100%")]
        [Display(Name = "��� ��������")]
        public double Weight { get; set; }

        [Display(Name = "�������� ��������")]
        [StringLength(500, ErrorMessage = "�������� �� ������ ��������� 500 ��������")]
        public string Description { get; set; }
    }
}