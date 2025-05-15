using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Criteria
{
    public class IndexModel : PageModel
    {
        private readonly IEvaluationService _evaluationService;
        private readonly IUserService _userService;

        public IndexModel(IEvaluationService evaluationService, IUserService userService)
        {
            _evaluationService = evaluationService;
            _userService = userService;
        }

        public List<EvaluationCriterion> Criteria { get; set; } = new List<EvaluationCriterion>();

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

            // Загружаем все критерии оценки
            var criteria = await _evaluationService.GetAllCriteriaAsync();
            Criteria = new List<EvaluationCriterion>(criteria);

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync(string name, double weight)
        {
            // Проверка аутентификации
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // Для отладки
            Console.WriteLine($"OnPostCreateAsync: Name = {name}, Weight = {weight}");

            // Базовая валидация
            if (string.IsNullOrWhiteSpace(name) || weight <= 0 || weight > 1)
            {
                StatusMessage = $"Ошибка: Некорректные данные. Проверьте название и вес критерия. (Name = {name}, Weight = {weight})";
                return RedirectToPage();
            }

            try
            {
                // Создаем новый критерий
                var criterion = new EvaluationCriterion
                {
                    Name = name,
                    Weight = weight,
                    CreatedAt = DateTime.Now,
                };

                await _evaluationService.CreateCriterionAsync(criterion);
                StatusMessage = $"Критерий '{name}' успешно создан с весом {weight:P0}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(int id, string name, double weight)
        {
            // Проверка аутентификации
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // Для отладки
            Console.WriteLine($"OnPostEditAsync: Id = {id}, Name = {name}, Weight = {weight}");

            // Базовая валидация
            if (id <= 0 || string.IsNullOrWhiteSpace(name) || weight <= 0 || weight > 1)
            {
                StatusMessage = $"Ошибка: Некорректные данные. Проверьте название и вес критерия. (Id = {id}, Name = {name}, Weight = {weight})";
                return RedirectToPage();
            }

            try
            {
                // Получаем существующий критерий
                var criterion = await _evaluationService.GetCriterionByIdAsync(id);
                if (criterion == null)
                {
                    StatusMessage = "Ошибка: Критерий не найден.";
                    return RedirectToPage();
                }

                // Обновляем данные
                criterion.Name = name;
                criterion.Weight = weight;
                criterion.UpdatedAt = DateTime.UtcNow;

                await _evaluationService.UpdateCriterionAsync(criterion);
                StatusMessage = $"Критерий '{name}' успешно обновлен с весом {weight:P0}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            // Проверка аутентификации
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // Базовая валидация
            if (id <= 0)
            {
                StatusMessage = "Ошибка: Некорректный идентификатор критерия.";
                return RedirectToPage();
            }

            try
            {
                // Проверяем, используется ли критерий в оценках
                var criterion = await _evaluationService.GetCriterionByIdAsync(id);
                if (criterion == null)
                {
                    StatusMessage = "Ошибка: Критерий не найден.";
                    return RedirectToPage();
                }

                // Удаляем критерий
                await _evaluationService.DeleteCriterionAsync(id);
                StatusMessage = $"Критерий '{criterion.Name}' успешно удален.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}