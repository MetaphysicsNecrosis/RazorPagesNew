using Microsoft.IdentityModel.Tokens;
using RazorPagesNew.Data;
using RazorPagesNew.Models;
using RazorPagesNew.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RazorPagesNew.Services.Implementation
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public TokenService(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        /// <summary>
        /// Генерирует JWT токен для пользователя
        /// </summary>
        public string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.Name)
            };

            // Добавляем сотрудника, если привязан
            if (user.EmployeeId.HasValue)
            {
                claims.Add(new Claim("EmployeeId", user.EmployeeId.Value.ToString()));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15), // Короткий срок жизни токена
                Issuer = _configuration["JWT:Issuer"],
                Audience = _configuration["JWT:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Генерирует токен обновления для пользователя
        /// </summary>
        public RefreshToken GenerateRefreshToken(int userId)
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                UserId = userId,
                Expires = DateTime.UtcNow.AddDays(7), // Долгий срок жизни refresh токена
                Created = DateTime.UtcNow
            };

            return refreshToken;
        }

        /// <summary>
        /// Получает ClaimsPrincipal из истекшего токена
        /// </summary>
        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // Не проверяем время истечения
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["JWT:Issuer"],
                ValidAudience = _configuration["JWT:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);

            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
    }
}
