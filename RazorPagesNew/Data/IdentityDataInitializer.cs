using Microsoft.AspNetCore.Identity;

namespace RazorPagesNew.Data
{
    public static class IdentityDataInitializer
    {
        public static void SeedData(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            SeedRoles(roleManager);
            SeedAdminUser(userManager);
        }

        private static void SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            // Создаем роли, если они не существуют
            string[] roleNames = { "Admin", "HR", "Manager", "Employee", "Evaluator", "User" };

            foreach (var roleName in roleNames)
            {
                if (!roleManager.RoleExistsAsync(roleName).Result)
                {
                    IdentityRole role = new IdentityRole { Name = roleName };
                    IdentityResult result = roleManager.CreateAsync(role).Result;
                    // Проверка результата не обязательна, но рекомендуется для обработки ошибок
                    if (!result.Succeeded)
                    {
                        throw new Exception($"Error creating role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }

        private static void SeedAdminUser(UserManager<IdentityUser> userManager)
        {
            // Создаем администратора, если он не существует
            string adminEmail = "admin@example.com";
            string adminPassword = "Admin@123456"; // В реальном проекте следует использовать более безопасный способ

            if (userManager.FindByEmailAsync(adminEmail).Result == null)
            {
                IdentityUser admin = new IdentityUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                IdentityResult result = userManager.CreateAsync(admin, adminPassword).Result;

                if (result.Succeeded)
                {
                    // Присваиваем роль администратора
                    userManager.AddToRoleAsync(admin, "Admin").Wait();
                }
                else
                {
                    throw new Exception($"Error creating admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}
