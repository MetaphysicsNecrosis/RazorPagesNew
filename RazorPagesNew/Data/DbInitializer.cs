using Microsoft.EntityFrameworkCore;
using RazorPagesNew.Models.Enums;
using RazorPagesNew.Models.Evaluate;
using RazorPagesNew.Models;

namespace RazorPagesNew.Data
{
    /// <summary>
    /// Класс для инициализации базы данных начальными данными
    /// </summary>
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Проверяем, нужно ли создавать миграции
            context.Database.Migrate();

            // Проверяем, есть ли уже данные в таблице ролей
            if (context.Roles.Any())
            {
                return; // База данных уже инициализирована
            }

            // Инициализация ролей
            var roles = new Role[]
            {
                new Role { Name = "Admin" },
                new Role { Name = "HR" },
                new Role { Name = "Evaluator" },
                new Role { Name = "User" }
            };

            context.Roles.AddRange(roles);
            context.SaveChanges();

            // Инициализация отделов
            var departments = new Department[]
            {
                new Department { Name = "IT" },
                new Department { Name = "HR" },
                new Department { Name = "Finance" },
                new Department { Name = "Marketing" }
            };

            context.Departments.AddRange(departments);
            context.SaveChanges();

            // Создание администратора
           /* var passwordHasher = new BCrypt.Net.BCrypt.HashPassword(); // Или любой другой хешер паролей*/
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                RoleId = roles.First(r => r.Name == "Admin").Id
            };

            context.Users.Add(adminUser);
            context.SaveChanges();

            // Инициализация критериев оценки
            var criteria = new EvaluationCriterion[]
            {
                new EvaluationCriterion { Name = "Производительность", Weight = 0.3 },
                new EvaluationCriterion { Name = "Командная работа", Weight = 0.2 },
                new EvaluationCriterion { Name = "Инициативность", Weight = 0.15 },
                new EvaluationCriterion { Name = "Коммуникация", Weight = 0.2 },
                new EvaluationCriterion { Name = "Дисциплина", Weight = 0.15 }
            };

            context.EvaluationCriteria.AddRange(criteria);
            context.SaveChanges();

            // Добавьте запись в лог аудита о создании системы
            var auditLog = new AuditLog
            {
                Username = "system",
                ActionType = ActionType.Create,
                EntityName = "System",
                EntityId = "0",
                Details = "Система инициализирована с начальными данными"
            };

            context.AuditLogs.Add(auditLog);
            context.SaveChanges();
        }
    }
}
