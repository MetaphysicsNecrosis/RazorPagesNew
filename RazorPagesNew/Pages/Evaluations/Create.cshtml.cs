using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Evaluations
{
    public class CreateModel : PageModel
    {
        private readonly MyApplicationDbContext _context;
        private readonly IEvaluationService _evaluationService;
        private readonly IUserService _userService;

        public CreateModel(
            MyApplicationDbContext context,
            IEvaluationService evaluationService,
            IUserService userService)
        {
            _context = context;
            _evaluationService = evaluationService;
            _userService = userService;
        }

        [BindProperty]
        public EmployeeEvaluation Evaluation { get; set; }

        [BindProperty]
        public WorkActivitySummary Summary { get; set; }

        [BindProperty]
        public List<CriterionScoreViewModel> CriteriaScores { get; set; }

        public SelectList EmployeeList { get; set; }

        public string ErrorMessage { get; set; }

        public class CriterionScoreViewModel
        {
            public int CriterionId { get; set; }
            public string CriterionName { get; set; }
            public double Weight { get; set; }
            public double Score { get; set; }
            public double WeightedScore => Weight * Score;
        }

        public async Task<IActionResult> OnGetAsync(int? employeeId = null)
        {
            // ��������� ������ ����������� ��� ������������� ������������� �������
            var employees = await _context.Employees
                .Select(e => new
                {
                    e.Id,
                    DisplayName = e.FullName + " (" + e.Position + ")"
                })
                .OrderBy(e => e.DisplayName)
                .ToListAsync();

            EmployeeList = new SelectList(employees, "Id", "DisplayName");

            // ������������� ������� ������
            Evaluation = new EmployeeEvaluation
            {
                EvaluationDate = DateTime.Now,
                Score = 0
            };

            // ��������� ���������� ����������, ���� ������� ID
            if (employeeId.HasValue)
            {
                Evaluation.EmployeeId = employeeId.Value;
            }

            // ������������� ������� ������ ����������
            Summary = new WorkActivitySummary
            {
                PeriodStart = DateTime.Now.AddMonths(-1),
                PeriodEnd = DateTime.Now
            };

            // �������� ��������� ������
            var criteria = await _context.EvaluationCriteria
                .OrderBy(c => c.Name)
                .ToListAsync();

            CriteriaScores = criteria.Select(c => new CriterionScoreViewModel
            {
                CriterionId = c.Id,
                CriterionName = c.Name,
                Weight = c.Weight,
                Score = 50 // ��������� �������� �� ��������� (������� ������)
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // ������������ ������ �����������
                var employees = await _context.Employees
                    .Select(e => new
                    {
                        e.Id,
                        DisplayName = e.FullName + " (" + e.Position + ")"
                    })
                    .OrderBy(e => e.DisplayName)
                    .ToListAsync();

                EmployeeList = new SelectList(employees, "Id", "DisplayName");
                return Page();
            }

            try
            {
                // �������� �������� ������������
                var currentUser = await _userService.GetCurrentUserAsync(User);
                if (currentUser == null)
                {
                    ErrorMessage = "�� ������� ���������� �������� ������������.";
                    return Page();
                }

                // ������������� ���������� � ���������
                Evaluation.EvaluatorId = currentUser.Id;
                Evaluation.OwnerId = currentUser.Id;
                Summary.OwnerId = currentUser.Id;
                Summary.EmployeeId = Evaluation.EmployeeId;

                // ��������� ������ ���������� ��� ������� ������
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // ���������� ������ ����������
                        Summary.CreatedAt = DateTime.Now;
                        await _context.WorkActivitySummaries.AddAsync(Summary);
                        await _context.SaveChangesAsync();

                        // ������������� ID ������ � ������
                        Evaluation.SummaryId = Summary.Id;

                        // ������������ �������� ������ �� ������ ���������� ������ �� ���������
                        double totalScore = 0;
                        double totalWeight = 0;

                        foreach (var criterionScore in CriteriaScores)
                        {
                            totalScore += criterionScore.Score * criterionScore.Weight;
                            totalWeight += criterionScore.Weight;
                        }

                        // ��������� �������� ���� (��������� ������, ����� ����� ����� ����� ���� �� ����� 1)
                        Evaluation.Score = totalWeight > 0 ? Math.Round(totalScore / totalWeight, 1) : 0;

                        // ������������� ���� ��������
                        Evaluation.CreatedAt = DateTime.Now;

                        // ���������� ������
                        await _context.EmployeeEvaluations.AddAsync(Evaluation);
                        await _context.SaveChangesAsync();

                        // ���������� ������ �� ���������
                        foreach (var criterionScore in CriteriaScores)
                        {
                            var score = new EvaluationScore
                            {
                                EvaluationId = Evaluation.Id,
                                CriterionId = criterionScore.CriterionId,
                                Score = criterionScore.Score,
                                CreatedAt = DateTime.Now
                            };

                            await _context.EvaluationScores.AddAsync(score);
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return RedirectToPage("./Details", new { id = Evaluation.Id });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ErrorMessage = $"������ ��� ���������� ������: {ex.Message}";

                        // ������������ ������ �����������
                        var employees = await _context.Employees
                            .Select(e => new
                            {
                                e.Id,
                                DisplayName = e.FullName + " (" + e.Position + ")"
                            })
                            .OrderBy(e => e.DisplayName)
                            .ToListAsync();

                        EmployeeList = new SelectList(employees, "Id", "DisplayName");
                        return Page();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"��������� ������: {ex.Message}";

                // ������������ ������ �����������
                var employees = await _context.Employees
                    .Select(e => new
                    {
                        e.Id,
                        DisplayName = e.FullName + " (" + e.Position + ")"
                    })
                    .OrderBy(e => e.DisplayName)
                    .ToListAsync();

                EmployeeList = new SelectList(employees, "Id", "DisplayName");
                return Page();
            }
        }
    }
}