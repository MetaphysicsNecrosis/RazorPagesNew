using RazorPagesNew.ModelsDb;
using System.Security.Claims;

namespace RazorPagesNew.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateJwtToken(User user);
        RefreshToken GenerateRefreshToken(int userId);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
