using Microsoft.EntityFrameworkCore;
using RazorPagesNew.Models.Enums;
using RazorPagesNew.Models.Import;
using RazorPagesNew.ModelsDb;
using RazorPagesNew.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace RazorPagesNew.Services.Implementation
{
    public class EmployeeImportService : Interfaces.IEmployeeImportService
    {
        private readonly MyApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public EmployeeImportService(
            MyApplicationDbContext context,
            IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Импортирует сотрудников из списка DTO
        /// </summary>
        public async Task<ImportResult> ImportEmployeesAsync(
            List<EmployeeImportDto> employees,
            int defaultDepartmentId,
            bool updateExisting,
            string username)
        {
            var result = new ImportResult
            {
                Success = true,
                ProcessedRows = 0,
                SuccessfulRows = 0,
                AddedCount = 0,
                UpdatedCount = 0,
                Message = "Импорт успешно завершен"
            };

            if (employees == null || !employees.Any())
            {
                result.Success = false;
                result.Message = "Не найдены данные для импорта";
                return result;
            }

            // Загружаем все отделы для поиска по имени
            var departments = await _context.Departments.ToListAsync();
            var defaultDepartment = await _context.Departments.FindAsync(defaultDepartmentId);

            if (defaultDepartment == null)
            {
                result.Success = false;
                result.Message = "Ошибка: Не найден отдел по умолчанию";
                return result;
            }

            // Получаем Id текущего пользователя
            var ownerId = 1; // По умолчанию, если не найден
            if (!string.IsNullOrEmpty(username))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null)
                {
                    ownerId = user.Id;
                }
            }

            // Загружаем существующих сотрудников по email для обновления
            var existingEmployeesByEmail = new Dictionary<string, Employee>();
            if (updateExisting)
            {
                var emails = employees
                    .Where(e => !string.IsNullOrEmpty(e.Email))
                    .Select(e => e.Email.ToLowerInvariant())
                    .Distinct()
                    .ToList();

                var existingEmployees = await _context.Employees
                    .Where(e => emails.Contains(e.Email.ToLower()))
                    .ToListAsync();

                foreach (var employee in existingEmployees)
                {
                    existingEmployeesByEmail[employee.Email.ToLowerInvariant()] = employee;
                }
            }

            // Массово обрабатываем сотрудников
            foreach (var dto in employees)
            {
                result.ProcessedRows++;

                try
                {
                    // Валидация данных
                    var validationResult = ValidateEmployeeDto(dto);
                    if (!validationResult.IsValid)
                    {
                        foreach (var error in validationResult.Errors)
                        {
                            result.Errors.Add(new ImportError
                            {
                                RowNumber = dto.RowNumber,
                                FieldName = error.FieldName,
                                ErrorMessage = error.ErrorMessage
                            });
                        }
                        continue;
                    }

                    // Поиск отдела по имени
                    int departmentId = defaultDepartmentId;
                    if (!string.IsNullOrEmpty(dto.Department))
                    {
                        var department = departments.FirstOrDefault(d =>
                            d.Name.Equals(dto.Department, StringComparison.OrdinalIgnoreCase));

                        if (department != null)
                        {
                            departmentId = department.Id;
                        }
                    }

                    // Проверяем существование сотрудника по email
                    if (!string.IsNullOrEmpty(dto.Email) &&
                        existingEmployeesByEmail.TryGetValue(dto.Email.ToLowerInvariant(), out var existingEmployee))
                    {
                        // Обновляем существующего сотрудника
                        if (updateExisting)
                        {
                            existingEmployee.FullName = dto.FullName;
                            existingEmployee.Phone = dto.Phone ?? existingEmployee.Phone;
                            existingEmployee.Position = dto.Position ?? existingEmployee.Position;
                            existingEmployee.DepartmentId = departmentId;

                            if (dto.HireDate.HasValue)
                            {
                                existingEmployee.HireDate = dto.HireDate.Value;
                            }

                            existingEmployee.UpdatedAt = DateTime.UtcNow;

                            _context.Employees.Update(existingEmployee);
                            result.UpdatedCount++;
                            result.SuccessfulRows++;
                        }
                        else
                        {
                            // Пропускаем существующего сотрудника, если обновление не разрешено
                            result.Errors.Add(new ImportError
                            {
                                RowNumber = dto.RowNumber,
                                FieldName = "Email",
                                ErrorMessage = "Сотрудник с таким email уже существует. Включите опцию 'Обновлять существующих сотрудников' для обновления данных."
                            });
                            continue;
                        }
                    }
                    else
                    {
                        // Создаем нового сотрудника
                        var newEmployee = new Employee
                        {
                            FullName = dto.FullName,
                            Email = dto.Email ?? $"employee{result.AddedCount + 1}@example.com",
                            Phone = dto.Phone ?? "",
                            Position = dto.Position ?? "Сотрудник",
                            DepartmentId = departmentId,
                            HireDate = dto.HireDate ?? DateTime.UtcNow,
                            OwnerId = ownerId,
                            VacationBalance = 28, // Стандартное значение отпуска
                            EmploymentType = 0, // Полная занятость по умолчанию
                            CreatedAt = DateTime.UtcNow
                        };

                        await _context.Employees.AddAsync(newEmployee);
                        result.AddedCount++;
                        result.SuccessfulRows++;
                    }
                }
                catch (Exception ex)
                {
                    // Записываем ошибку в результат
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = dto.RowNumber,
                        FieldName = "Обработка",
                        ErrorMessage = ex.Message
                    });
                }
            }

            // Сохраняем изменения в базе данных
            await _context.SaveChangesAsync();

            // Формируем итоговое сообщение
            if (result.ProcessedRows == 0)
            {
                result.Success = false;
                result.Message = "В файле не найдены данные о сотрудниках.";
            }
            else if (result.SuccessfulRows == 0)
            {
                result.Success = false;
                result.Message = $"Не удалось импортировать ни одного сотрудника из {result.ProcessedRows} записей.";
            }
            else if (result.SuccessfulRows < result.ProcessedRows)
            {
                result.Success = true;
                result.Message = $"Частично успешный импорт. Обработано {result.ProcessedRows} записей, успешно импортировано {result.SuccessfulRows}.";
            }
            else
            {
                result.Success = true;
                result.Message = $"Импорт успешно завершен. Обработано {result.ProcessedRows} записей.";
            }

            // Логируем результат импорта
            await _auditLogService.LogActivityAsync(
                username ?? "system",
                ActionType.Import,
                "Employee",
                "0",
                $"Импортировано сотрудников: {result.SuccessfulRows}/{result.ProcessedRows}. Добавлено: {result.AddedCount}, обновлено: {result.UpdatedCount}"
            );

            return result;
        }

        /// <summary>
        /// Валидирует данные о сотруднике
        /// </summary>
        private Models.Import.ValidationResult ValidateEmployeeDto(EmployeeImportDto dto)
        {
            var result = new Models.Import.ValidationResult { IsValid = true };

            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    RowNumber = dto.RowNumber,
                    FieldName = "ФИО",
                    ErrorMessage = "ФИО сотрудника обязательно для заполнения"
                });
            }

            // Проверка формата email
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                if (!Regex.IsMatch(dto.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        RowNumber = dto.RowNumber,
                        FieldName = "Email",
                        ErrorMessage = "Неверный формат email"
                    });
                }
            }

            // Проверка даты приема
            if (dto.HireDate.HasValue && dto.HireDate > DateTime.Now)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    RowNumber = dto.RowNumber,
                    FieldName = "Дата приема",
                    ErrorMessage = "Дата приема не может быть в будущем"
                });
            }

            return result;
        }
    }
}