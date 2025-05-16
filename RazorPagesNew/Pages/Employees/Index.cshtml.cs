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

namespace RazorPagesNew.Pages.Employees
{
    public class IndexModel : PageModel
    {
        private readonly IEmployeeService _employeeService;
        private readonly IEvaluationService _evaluationService;
        private readonly MyApplicationDbContext _context;
        public IndexModel(
            IEmployeeService employeeService,
            IEvaluationService evaluationService,
            MyApplicationDbContext context)
        {
            _employeeService = employeeService;
            _evaluationService = evaluationService;
            _context = context;
        }

        public string CurrentFilter { get; set; }
        public string CurrentSort { get; set; }
        public int? DepartmentId { get; set; }
        public SelectList DepartmentList { get; set; }
        public string NameSort { get; set; }
        public string DepartmentSort { get; set; }
        public string ScoreSort { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public List<EmployeeCardViewModel> Employees { get; set; } = new List<EmployeeCardViewModel>();

        public async Task<IActionResult> OnGetAsync(
            string sortOrder,
            string currentFilter,
            string searchTerm,
            int? departmentId,
            int? pageIndex)
        {
            // Сохраняем параметры фильтрации
            CurrentSort = sortOrder;
            DepartmentId = departmentId;
            CurrentPage = pageIndex ?? 1;

            // Устанавливаем направление сортировки
            NameSort = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            DepartmentSort = sortOrder == "department" ? "department_desc" : "department";
            ScoreSort = sortOrder == "score" ? "score_desc" : "score";

            // Если новый поисковый запрос, возвращаемся на первую страницу
            if (searchTerm != null)
            {
                CurrentPage = 1;
            }
            else
            {
                searchTerm = currentFilter;
            }

            CurrentFilter = searchTerm;

            // Загружаем список отделов для фильтрации
            await LoadDepartmentsAsync();

            // Получаем сотрудников по фильтрам
            IEnumerable<Employee> employeesQuery;

            if (DepartmentId.HasValue)
            {
                employeesQuery = await _employeeService.GetEmployeesByDepartmentAsync(DepartmentId.Value);
            }
            else if (!string.IsNullOrEmpty(searchTerm))
            {
                employeesQuery = await _employeeService.SearchEmployeesAsync(searchTerm);
            }
            else
            {
                employeesQuery = await _employeeService.GetAllEmployeesAsync();
            }

            // Сортировка результатов
            employeesQuery = ApplySorting(employeesQuery, sortOrder);

            // Пагинация
            int pageSize = 12; // Увеличили количество элементов на странице для карточек
            var totalItems = employeesQuery.Count();
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var paginatedList = employeesQuery
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Преобразуем модель данных в ViewModel
            Employees = await TransformToViewModelAsync(paginatedList);

            return Page();
        }

        private async Task LoadDepartmentsAsync()
        {
            var departments = await _context.Departments.ToListAsync();
            DepartmentList = new SelectList(departments, "Id", "Name");
        }

        private IEnumerable<Employee> ApplySorting(IEnumerable<Employee> query, string sortOrder)
        {
            return sortOrder switch
            {
                "name_desc" => query.OrderByDescending(e => e.FullName),
                "department" => query.OrderBy(e => e.Department?.Name),
                "department_desc" => query.OrderByDescending(e => e.Department?.Name),
                "score" => query.OrderBy(e => e.EmployeeEvaluations.Any() ? e.EmployeeEvaluations.Average(ev => ev.Score) : 0),
                "score_desc" => query.OrderByDescending(e => e.EmployeeEvaluations.Any() ? e.EmployeeEvaluations.Average(ev => ev.Score) : 0),
                _ => query.OrderBy(e => e.FullName),
            };
        }

        private async Task<List<EmployeeCardViewModel>> TransformToViewModelAsync(List<Employee> employees)
        {
            var viewModelList = new List<EmployeeCardViewModel>();

            foreach (var employee in employees)
            {
                var averageScore = 0.0;
                if (employee.EmployeeEvaluations != null && employee.EmployeeEvaluations.Any())
                {
                    averageScore = employee.EmployeeEvaluations.Average(e => e.Score);
                }
                else
                {
                    // Если коллекция оценок еще не загружена, вычисляем средний балл из базы
                    var evaluations = await _context.EmployeeEvaluations
                        .Where(e => e.EmployeeId == employee.Id)
                        .ToListAsync();

                    if (evaluations.Any())
                    {
                        averageScore = evaluations.Average(e => e.Score);
                    }
                }

                viewModelList.Add(new EmployeeCardViewModel
                {
                    Id = employee.Id,
                    FullName = employee.FullName,
                    Position = employee.Position,
                    Department = employee.Department,
                    Email = employee.Email,
                    Phone = employee.Phone,
                    AverageScore = averageScore,
                    PhotoPath = employee.PhotoPath
                });
            }

            return viewModelList;
        }
    }

    public class EmployeeCardViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Position { get; set; }
        public Department Department { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public double AverageScore { get; set; }
        public string PhotoPath { get; set; }
    }
}