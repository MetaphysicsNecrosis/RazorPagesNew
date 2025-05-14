using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace RazorPagesNew.Data
{
    public class MyIdentityDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public MyIdentityDbContext(DbContextOptions<MyIdentityDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Здесь можно настроить схему таблиц Identity, если нужно
            // Например, изменить имена таблиц:
            modelBuilder.Entity<IdentityUser>().ToTable("Users", "Identity");
            modelBuilder.Entity<IdentityRole>().ToTable("Roles", "Identity");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", "Identity");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", "Identity");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", "Identity");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", "Identity");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", "Identity");
        }
    }
}
