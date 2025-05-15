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
            // �������� ��������������
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // �������� �������� ������������
            var currentUser = await _userService.GetCurrentUserAsync(User);
            if (currentUser == null)
                return RedirectToPage("/Account/Login");

            // ��������� ��� �������� ������
            var criteria = await _evaluationService.GetAllCriteriaAsync();
            Criteria = new List<EvaluationCriterion>(criteria);

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync(string name, double weight)
        {
            // �������� ��������������
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // ��� �������
            Console.WriteLine($"OnPostCreateAsync: Name = {name}, Weight = {weight}");

            // ������� ���������
            if (string.IsNullOrWhiteSpace(name) || weight <= 0 || weight > 1)
            {
                StatusMessage = $"������: ������������ ������. ��������� �������� � ��� ��������. (Name = {name}, Weight = {weight})";
                return RedirectToPage();
            }

            try
            {
                // ������� ����� ��������
                var criterion = new EvaluationCriterion
                {
                    Name = name,
                    Weight = weight,
                    CreatedAt = DateTime.Now,
                };

                await _evaluationService.CreateCriterionAsync(criterion);
                StatusMessage = $"�������� '{name}' ������� ������ � ����� {weight:P0}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"������: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(int id, string name, double weight)
        {
            // �������� ��������������
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // ��� �������
            Console.WriteLine($"OnPostEditAsync: Id = {id}, Name = {name}, Weight = {weight}");

            // ������� ���������
            if (id <= 0 || string.IsNullOrWhiteSpace(name) || weight <= 0 || weight > 1)
            {
                StatusMessage = $"������: ������������ ������. ��������� �������� � ��� ��������. (Id = {id}, Name = {name}, Weight = {weight})";
                return RedirectToPage();
            }

            try
            {
                // �������� ������������ ��������
                var criterion = await _evaluationService.GetCriterionByIdAsync(id);
                if (criterion == null)
                {
                    StatusMessage = "������: �������� �� ������.";
                    return RedirectToPage();
                }

                // ��������� ������
                criterion.Name = name;
                criterion.Weight = weight;
                criterion.UpdatedAt = DateTime.UtcNow;

                await _evaluationService.UpdateCriterionAsync(criterion);
                StatusMessage = $"�������� '{name}' ������� �������� � ����� {weight:P0}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"������: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            // �������� ��������������
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            // ������� ���������
            if (id <= 0)
            {
                StatusMessage = "������: ������������ ������������� ��������.";
                return RedirectToPage();
            }

            try
            {
                // ���������, ������������ �� �������� � �������
                var criterion = await _evaluationService.GetCriterionByIdAsync(id);
                if (criterion == null)
                {
                    StatusMessage = "������: �������� �� ������.";
                    return RedirectToPage();
                }

                // ������� ��������
                await _evaluationService.DeleteCriterionAsync(id);
                StatusMessage = $"�������� '{criterion.Name}' ������� ������.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"������: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}