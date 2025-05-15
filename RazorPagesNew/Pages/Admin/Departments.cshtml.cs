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
                StatusMessage = "Ошибка: Название отдела не может быть пустым";
                await LoadDepartmentsAsync();
                return Page();
            }

            // Проверяем, существует ли уже отдел с таким названием
            if (await _dbContext.Departments.AnyAsync(d => d.Name == name))
            {
                StatusMessage = "Ошибка: Отдел с таким названием уже существует";
                await LoadDepartmentsAsync();
                return Page();
            }

            // Создаем новый отдел
            var department = new Department
            {
                Name = name,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Departments.AddAsync(department);
            await _dbContext.SaveChangesAsync();

            StatusMessage = $"Отдел '{name}' успешно создан";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(int id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                StatusMessage = "Ошибка: Название отдела не может быть пустым";
                await LoadDepartmentsAsync();
                return Page();
            }

            // Проверяем, существует ли уже отдел с таким названием
            if (await _dbContext.Departments.AnyAsync(d => d.Name == name && d.Id != id))
            {
                StatusMessage = "Ошибка: Отдел с таким названием уже существует";
                await LoadDepartmentsAsync();
                return Page();
            }

            // Получаем отдел из базы данных
            var department = await _dbContext.Departments.FindAsync(id);
            if (department == null)
            {
                StatusMessage = "Ошибка: Отдел не найден";
                await LoadDepartmentsAsync();
                return Page();
            }

            // Обновляем отдел
            department.Name = name;
            department.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            StatusMessage = $"Отдел '{name}' успешно обновлен";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            // Получаем отдел из базы данных
            var department = await _dbContext.Departments
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                StatusMessage = "Ошибка: Отдел не найден";
                await LoadDepartmentsAsync();
                return Page();
            }

            // Проверяем, есть ли сотрудники в отделе
            if (department.Employees.Any())
            {
                StatusMessage = $"Ошибка: Невозможно удалить отдел '{department.Name}', так как в нем есть сотрудники";
                await LoadDepartmentsAsync();
                return Page();
            }

            // Удаляем отдел
            _dbContext.Departments.Remove(department);
            await _dbContext.SaveChangesAsync();

            StatusMessage = $"Отдел '{department.Name}' успешно удален";
            return RedirectToPage();
        }

        private async Task LoadDepartmentsAsync()
        {
            // Получаем все отделы с включением сотрудников
            var departments = await _dbContext.Departments
                .Include(d => d.Employees)
                .ThenInclude(e => e.EmployeeEvaluations)
                .ToListAsync();

            // Подготавливаем данные для отображения
            Departments = departments.Select(d => new DepartmentViewModel
            {
                Id = d.Id,
                Name = d.Name,
                EmployeeCount = d.Employees.Count,
                AverageScore = CalculateAverageScore(d.Employees),
                HasEmployees = d.Employees.Any(),
                CreatedAt = d.CreatedAt
            }).ToList();

            // Подготавливаем данные для графика
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