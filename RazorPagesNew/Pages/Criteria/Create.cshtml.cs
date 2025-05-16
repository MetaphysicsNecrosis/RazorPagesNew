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

        // Свойство для сообщений об ошибках и успешных операциях
        [TempData]
        public string StatusMessage { get; set; }

        // Список существующих критериев для предварительного просмотра
        public List<EvaluationCriterion> ExistingCriteria { get; set; }

        // Общий вес существующих критериев
        public double TotalExistingWeight { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Проверяем права доступа - только администраторы и HR могут создавать критерии
            if (!User.IsInRole("Admin") && !User.IsInRole("HR"))
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            // Загружаем список существующих критериев для отображения
            ExistingCriteria = (await _evaluationService.GetAllCriteriaAsync()).ToList();

            // Вычисляем общий вес существующих критериев
            TotalExistingWeight = ExistingCriteria.Sum(c => c.Weight);

            // Инициализируем модель представления
            CriterionViewModel = new CriterionCreateViewModel
            {
                // Устанавливаем рекомендуемый вес нового критерия
                Weight = Math.Round(GetRecommendedWeight(), 2)
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Проверяем права доступа
            if (!User.IsInRole("Admin") && !User.IsInRole("HR"))
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            // Проверяем валидность модели
            if (!ModelState.IsValid)
            {
                // Перезагружаем данные для страницы
                ExistingCriteria = (await _evaluationService.GetAllCriteriaAsync()).ToList();
                TotalExistingWeight = ExistingCriteria.Sum(c => c.Weight);
                return Page();
            }

            // Проверка на уникальность названия критерия
            var existingCriterion = _context.EvaluationCriteria.FirstOrDefault(c => c.Name == CriterionViewModel.Name);
            if (existingCriterion != null)
            {
                ModelState.AddModelError("CriterionViewModel.Name", "Критерий с таким названием уже существует.");
                ExistingCriteria = (await _evaluationService.GetAllCriteriaAsync()).ToList();
                TotalExistingWeight = ExistingCriteria.Sum(c => c.Weight);
                return Page();
            }

            // Получаем текущего пользователя
            var currentUser = await _userService.GetCurrentUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Преобразуем модель представления в доменную модель
            var criterion = new EvaluationCriterion
            {
                Name = CriterionViewModel.Name,
                Weight = CriterionViewModel.Weight,
             /*   Description = CriterionViewModel.Description,*/
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                // Сохраняем новый критерий
                await _evaluationService.CreateCriterionAsync(criterion);

                // Логируем действие
                await _auditLogService.LogActivityAsync(
                    currentUser.Username,
                    Models.Enums.ActionType.Create,
                    "EvaluationCriterion",
                    criterion.Id.ToString(),
                    $"Создан новый критерий оценки: {criterion.Name} с весом: {criterion.Weight:P0}"
                );

                // Устанавливаем сообщение об успешном создании
                StatusMessage = $"Критерий '{criterion.Name}' успешно создан.";

                // Перенаправляем на страницу со списком критериев
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                // Обрабатываем ошибки
                StatusMessage = $"Ошибка: Не удалось создать критерий. {ex.Message}";

                // Перезагружаем данные для страницы
                ExistingCriteria = (await _evaluationService.GetAllCriteriaAsync()).ToList();
                TotalExistingWeight = ExistingCriteria.Sum(c => c.Weight);

                return Page();
            }
        }

        // Вычисляет рекомендуемый вес для нового критерия
        private double GetRecommendedWeight()
        {
            if (ExistingCriteria == null || !ExistingCriteria.Any())
            {
                return 1.0; // Если нет критериев, то вес 100%
            }

            double currentTotalWeight = ExistingCriteria.Sum(c => c.Weight);

            if (currentTotalWeight >= 1.0)
            {
                // Если общий вес уже 100% или больше, предлагаем уменьшить веса существующих критериев
                return 0.1; // Предлагаем вес 10%
            }

            // Иначе предлагаем заполнить оставшийся вес
            return Math.Min(0.3, 1.0 - currentTotalWeight); // Не более 30%
        }
    }

    // Модель представления для создания критерия
    public class CriterionCreateViewModel
    {
        [Required(ErrorMessage = "Название критерия обязательно")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Название должно содержать от 3 до 100 символов")]
        [Display(Name = "Название критерия")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Вес критерия обязателен")]
        [Range(0.01, 1.0, ErrorMessage = "Вес должен быть от 1% до 100%")]
        [Display(Name = "Вес критерия")]
        public double Weight { get; set; }

        [Display(Name = "Описание критерия")]
        [StringLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
        public string Description { get; set; }
    }
}