using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesNew.Pages.Admin
{
   /* [Authorize(Roles = "Admin")]*/
    public class DepartmentsModel : PageModel
    {
        private readonly MyApplicationDbContext _dbContext;
        private readonly IEmployeeService _employeeService;

        [TempData]
        public string StatusMessage { get; set; }

        public List<DepartmentViewModel> Departments { get; set; } = new List<DepartmentViewModel>();
        public ChartDataViewModel ChartData { get; set; } = new ChartDataViewModel();

        public DepartmentsModel(
            MyApplicationDbContext dbContext,
            IEmployeeService employeeService)
        {
            _dbContext = dbContext;
            _employeeService = employeeService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDepartmentsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                StatusMessage = "������: �������� ������ �� ����� ���� ������";
                await LoadDepartmentsAsync();
                return Page();
            }

            // ���������, ���������� �� ��� ����� � ����� ���������
            if (await _dbContext.Departments.AnyAsync(d => d.Name == name))
            {
                StatusMessage = "������: ����� � ����� ��������� ��� ����������";
                await LoadDepartmentsAsync();
                return Page();
            }

            // ������� ����� �����
            var department = new Department
            {
                Name = name,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Departments.AddAsync(department);
            await _dbContext.SaveChangesAsync();

            StatusMessage = $"����� '{name}' ������� ������";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(int id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                StatusMessage = "������: �������� ������ �� ����� ���� ������";
                await LoadDepartmentsAsync();
                return Page();
            }

            // ���������, ���������� �� ��� ����� � ����� ���������
            if (await _dbContext.Departments.AnyAsync(d => d.Name == name && d.Id != id))
            {
                StatusMessage = "������: ����� � ����� ��������� ��� ����������";
                await LoadDepartmentsAsync();
                return Page();
            }

            // �������� ����� �� ���� ������
            var department = await _dbContext.Departments.FindAsync(id);
            if (department == null)
            {
                StatusMessage = "������: ����� �� ������";
                await LoadDepartmentsAsync();
                return Page();
            }

            // ��������� �����
            department.Name = name;
            department.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            StatusMessage = $"����� '{name}' ������� ��������";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            // �������� ����� �� ���� ������
            var department = await _dbContext.Departments
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                StatusMessage = "������: ����� �� ������";
                await LoadDepartmentsAsync();
                return Page();
            }

            // ���������, ���� �� ���������� � ������
            if (department.Employees.Any())
            {
                StatusMessage = $"������: ���������� ������� ����� '{department.Name}', ��� ��� � ��� ���� ����������";
                await LoadDepartmentsAsync();
                return Page();
            }

            // ������� �����
            _dbContext.Departments.Remove(department);
            await _dbContext.SaveChangesAsync();

            StatusMessage = $"����� '{department.Name}' ������� ������";
            return RedirectToPage();
        }

        private async Task LoadDepartmentsAsync()
        {
            // �������� ��� ������ � ���������� �����������
            var departments = await _dbContext.Departments
                .Include(d => d.Employees)
                .ThenInclude(e => e.EmployeeEvaluations)
                .ToListAsync();

            // �������������� ������ ��� �����������
            Departments = departments.Select(d => new DepartmentViewModel
            {
                Id = d.Id,
                Name = d.Name,
                EmployeeCount = d.Employees.Count,
                AverageScore = CalculateAverageScore(d.Employees),
                HasEmployees = d.Employees.Any(),
                CreatedAt = d.CreatedAt
            }).ToList();

            // �������������� ������ ��� �������
            ChartData = new ChartDataViewModel
            {
                Labels = Departments.Select(d => d.Name).ToList(),
                Data = Departments.Select(d => d.EmployeeCount).ToList()
            };
        }

        private double CalculateAverageScore(ICollection<Employee> employees)
        {
            if (!employees.Any() || !employees.Any(e => e.EmployeeEvaluations.Any()))
                return 0;

            double totalScore = 0;
            int evaluationsCount = 0;

            foreach (var employee in employees)
            {
                if (employee.EmployeeEvaluations.Any())
                {
                    totalScore += employee.EmployeeEvaluations.Average(e => e.Score);
                    evaluationsCount++;
                }
            }

            return evaluationsCount > 0 ? Math.Round(totalScore / evaluationsCount, 1) : 0;
        }

        public class DepartmentViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int EmployeeCount { get; set; }
            public double AverageScore { get; set; }
            public bool HasEmployees { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class ChartDataViewModel
        {
            public List<string> Labels { get; set; } = new List<string>();
            public List<int> Data { get; set; } = new List<int>();
        }
    }
}