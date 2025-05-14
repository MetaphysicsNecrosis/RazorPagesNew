using Microsoft.EntityFrameworkCore;
using RazorPagesNew.Models.Enums;
using RazorPagesNew.Models.Evaluate;
using RazorPagesNew.Models;
using Xunit;

namespace RazorPagesNew.Data.Tests
{
    public class ApplicationDbContextTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Каждый раз новая БД
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task CanAddAndRetrieveEmployee()
        {
            using var context = GetInMemoryDbContext();

            var department = new Department { Name = "IT" };
            var employee = new Employee
            {
                FullName = "Иван Иванов",
                Email = "ivan@example.com",
                Department = department,
                Position = "Программист",
                HireDate = DateTime.Today,
                VacationBalance = 10,
                SickLeaveUsed = 0,
                EmploymentType = EmploymentType.FullTime
            };

            context.Employees.Add(employee);
            await context.SaveChangesAsync();

            var savedEmployee = await context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == "ivan@example.com");

            Assert.NotNull(savedEmployee);
            Assert.Equal("Иван Иванов", savedEmployee.FullName);
            Assert.Equal("IT", savedEmployee.Department.Name);
        }

        [Fact]
        public async Task SaveChanges_ShouldSetCreatedAtAndUpdatedAt()
        {
            using var context = GetInMemoryDbContext();

            var user = new User { Username = "testuser", PasswordHash = "hash" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var saved = await context.Users.FirstOrDefaultAsync(u => u.Username == "testuser");
            var createdAt = context.Entry(saved).Property<DateTime>("CreatedAt").CurrentValue;

            Assert.NotEqual(default, createdAt);
        }


        [Fact]
        public async Task CanAddEvaluationWithScores()
        {
            using var context = GetInMemoryDbContext();

            var employee = new Employee
            {
                FullName = "Анна Смирнова",
                Email = "anna@example.com",
                Department = new Department { Name = "HR" },
                Position = "HR менеджер",
                HireDate = DateTime.UtcNow,
                VacationBalance = 5,
                EmploymentType = EmploymentType.PartTime
            };

            var criterion = new EvaluationCriterion { Name = "Качество работы", Weight = 1.0 };
            var evaluation = new EmployeeEvaluation
            {
                Employee = employee,
                Evaluator = new User { Username = "manager" },
                EvaluationDate = DateTime.UtcNow,
                Scores = new[]
                {
                new EvaluationScore
                {
                    Criterion = criterion,
                    Score = 4
                }
            }
            };

            context.EmployeeEvaluations.Add(evaluation);
            await context.SaveChangesAsync();

            var savedEval = await context.EmployeeEvaluations
                .Include(e => e.Scores)
                .ThenInclude(s => s.Criterion)
                .FirstOrDefaultAsync();

            Assert.NotNull(savedEval);
            Assert.Single(savedEval.Scores);
            Assert.Equal("Качество работы", savedEval.Scores.First().Criterion.Name);
        }
    }
}
