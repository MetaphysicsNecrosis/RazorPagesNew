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
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace RazorPagesNew.Pages.Employees
{
    public class CreateModel : PageModel
    {
        private readonly IEmployeeService _employeeService;
        private readonly MyApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CreateModel(
            IEmployeeService employeeService,
            MyApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IUserService userService,
            IWebHostEnvironment webHostEnvironment)
        {
            _employeeService = employeeService;
            _context = context;
            _userManager = userManager;
            _userService = userService;
            _webHostEnvironment = webHostEnvironment;
        }

        [BindProperty]
        public EmployeeCreateViewModel Employee { get; set; } = new EmployeeCreateViewModel();

        [BindProperty]
        public IFormFile PhotoUpload { get; set; }

        public SelectList DepartmentList { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDepartmentsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            /*if (!ModelState.IsValid)
            {
                await LoadDepartmentsAsync();
                return Page();
            }*/

            var currentUser = await _userService.GetByIdAsync(_userManager.GetUserId(User));

            // ��������� �������� ����������
            string photoPath = null;
            if (PhotoUpload != null && PhotoUpload.Length > 0)
            {
                photoPath = await SavePhotoAsync(PhotoUpload);
            }

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
                PhotoPath = photoPath,
                EmploymentType = Employee.EmploymentType,
                OwnerId = currentUser.Id,
     /*           Status = Employee.Status,
                Notes = Employee.Notes,*/
                CreatedAt = DateTime.Now
            };

            try
            {
                await _employeeService.CreateEmployeeAsync(newEmployee);
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"�� ������� ������� ����������: {ex.Message}");
                await LoadDepartmentsAsync();
                return Page();
            }
        }

        private async Task<string> SavePhotoAsync(IFormFile photo)
        {
            // ������� ���������� ��� �����
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(photo.FileName);

            // ���� � ����� ��� �������� ����������
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "employees");

            // ������� ����������, ���� ��� �� ����������
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // ������ ���� � �����
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // ��������� ����
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(fileStream);
            }

            // ���������� ������������� ���� ��� �������� � ��
            return "/images/employees/" + uniqueFileName;
        }

        private async Task LoadDepartmentsAsync()
        {
            var departments = await _context.Departments.ToListAsync();
            DepartmentList = new SelectList(departments, "Id", "Name");
        }
    }

    public class EmployeeCreateViewModel
    {
        [Required(ErrorMessage = "��� ����������� ��� ����������")]
        [Display(Name = "���")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "��������� ����������� ��� ����������")]
        [Display(Name = "���������")]
        public string Position { get; set; }

        [Required(ErrorMessage = "Email ���������� ��� ����������")]
        [EmailAddress(ErrorMessage = "�������� ������ Email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "�������")]
        [Phone(ErrorMessage = "�������� ������ ��������")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "�������� �����")]
        [Display(Name = "�����")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "���� ����� �����������")]
        [Display(Name = "���� �����")]
        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; } = DateTime.Now;

        [Display(Name = "������")]
        public string Status { get; set; } = "Active";

        [Display(Name = "������ �������")]
        [Range(0, 100, ErrorMessage = "������ ������� ������ ���� �� 0 �� 100")]
        public double VacationBalance { get; set; } = 28;

        [Display(Name = "����������")]
        public string Notes { get; set; }

        // �������������� ���� ��� ������������ Employee
        [Display(Name = "�������������� ����������")]
        public double SickLeaveUsed { get; set; }

        [Display(Name = "����")]
        public string PhotoPath { get; set; }

        [Required(ErrorMessage = "��� ��������� ����������")]
        [Display(Name = "��� ���������")]
        public int EmploymentType { get; set; }

        [Required]
        [Display(Name = "ID ���������")]
        public int OwnerId { get; set; }
    }
}