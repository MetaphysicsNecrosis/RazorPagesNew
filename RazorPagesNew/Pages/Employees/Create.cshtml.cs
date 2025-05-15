using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Identity;

namespace RazorPagesNew.Pages.Employees
{
    public class CreateModel : PageModel
    {
        private readonly IEmployeeService _employeeService;
        private readonly MyApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserService _userService;

        public CreateModel(IEmployeeService employeeService, MyApplicationDbContext context, UserManager<IdentityUser> userManager, IUserService userService)
        {
            _employeeService = employeeService;
            _context = context;
            _userManager = userManager;
            _userService = userService;
        }

        [BindProperty]
        public EmployeeCreateViewModel Employee { get; set; } = new EmployeeCreateViewModel();

        public SelectList DepartmentList { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDepartmentsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDepartmentsAsync();
                return Page();
            }
            var currentUser = await _userService.GetByIdAsync(_userManager.GetUserId(User));
            Console.WriteLine(currentUser.Id + "-----------------------------------------------------");
            var newEmployee = new Employee
            {
                FullName = Employee.FullName,
                Position = Employee.Position,
                Email = Employee.Email,
                Phone = Employee.Phone,
                DepartmentId = Employee.DepartmentId,
                HireDate = Employee.HireDate,
                VacationBalance = Employee.VacationBalance,
                SickLeaveUsed = Employee.SickLeaveUsed,
                PhotoPath = Employee.PhotoPath,
                EmploymentType = Employee.EmploymentType,
                OwnerId = currentUser.Id,
/*                Status = Employee.Status,
                Notes = Employee.Notes*/
            };

            try
            {
                await _employeeService.CreateEmployeeAsync(newEmployee);
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Не удалось создать сотрудника: {ex.Message}");
                await LoadDepartmentsAsync();
                return Page();
            }
        }

        private async Task LoadDepartmentsAsync()
        {
            var departments = await _context.Departments.ToListAsync();
            DepartmentList = new SelectList(departments, "Id", "Name");
        }
    }

    public class EmployeeCreateViewModel
    {
        [Required(ErrorMessage = "ФИО обязательно для заполнения")]
        [Display(Name = "ФИО")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Должность обязательна для заполнения")]
        [Display(Name = "Должность")]
        public string Position { get; set; }

        [Required(ErrorMessage = "Email обязателен для заполнения")]
        [EmailAddress(ErrorMessage = "Неверный формат Email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Телефон")]
        [Phone(ErrorMessage = "Неверный формат телефона")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Выберите отдел")]
        [Display(Name = "Отдел")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Дата найма обязательна")]
        [Display(Name = "Дата найма")]
        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; } = DateTime.Now;

        [Display(Name = "Статус")]
        public string Status { get; set; } = "Active";

        [Display(Name = "Баланс отпуска")]
        [Range(0, 100, ErrorMessage = "Баланс отпуска должен быть от 0 до 100")]
        public double VacationBalance { get; set; } = 28;

        [Display(Name = "Примечания")]
        public string Notes { get; set; }

        // Дополнительные поля для соответствия Employee
        [Display(Name = "Использованный больничный")]
        public double SickLeaveUsed { get; set; }

        [Display(Name = "Фото")]
        public string PhotoPath { get; set; }

        [Required(ErrorMessage = "Тип занятости обязательна")]
        [Display(Name = "Тип занятости")]
        public int EmploymentType { get; set; }

        [Required]
        [Display(Name = "ID владельца")]
        public int OwnerId { get; set; }
    }
}
