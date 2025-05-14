using RazorPagesNew.Models;
using System.Security.Claims;

namespace RazorPagesNew.Services.Interfaces
{
    public interface IUserService
    {
        Task<User> AuthenticateAsync(string username, string password);
        Task<User> GetByIdAsync(int id);
        Task<User> GetByUsernameAsync(string username);
        Task<bool> CreateUserAsync(User user, string password);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> IsInRoleAsync(int userId, string roleName);
        Task<IEnumerable<User>> GetUsersInRoleAsync(string roleName);
        Task<User> GetCurrentUserAsync(ClaimsPrincipal userPrincipal);
    }
}
