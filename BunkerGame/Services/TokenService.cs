using BunkerGame.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BunkerGame.Services; // Или Helpers, если используешь хелперы

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    DateTime GenerateAccessTokenExpiry();
    DateTime GenerateRefreshTokenExpiry();
}

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateAccessToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Используем Id пользователя
            new Claim(ClaimTypes.Name, user.UserName ?? user.Email), // Имя пользователя
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Уникальный идентификатор токена (jti)
            // Можно добавить другие утверждения (claims) при необходимости, например, Email
            // new Claim(ClaimTypes.Email, user.Email),
        };

        // Если у тебя будут роли, их можно добавить сюда:
        // claims.Add(new Claim(ClaimTypes.Role, userRole));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = GenerateAccessTokenExpiry(),
            Issuer = _jwtSettings.ValidIssuer,
            Audience = _jwtSettings.ValidAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Генерируем случайный токен (например, 32 байта, закодированные в Base64)
        // Используем криптографически безопасный генератор
        var randomBytes = new byte[32]; // 256 бит
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }

    public DateTime GenerateAccessTokenExpiry()
    {
        return DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
    }

    public DateTime GenerateRefreshTokenExpiry()
    {
        return DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
    }
}