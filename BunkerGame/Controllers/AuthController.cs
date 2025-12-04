// Controllers/AuthController.cs

using BunkerGame.Data;
using BunkerGame.Models;
using BunkerGame.DTOs.Auth;
using BunkerGame.Services; // Добавлено
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Добавлено для работы с DBContext

namespace BunkerGame.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ApplicationDbContext _context; // Добавлено для работы с RefreshToken
    private readonly ITokenService _tokenService; // Добавлено

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ApplicationDbContext context, // Добавлено
        ITokenService tokenService) // Добавлено
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context; // Добавлено
        _tokenService = tokenService; // Добавлено
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = new User { UserName = model.Email, Email = model.Email, Name = model.Name };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // --- НОВОЕ: Генерация и сохранение Refresh токена ---
            var (accessToken, refreshToken) = await GenerateTokensAndSetCookieAsync(user);
            // --- КОНЕЦ НОВОГО ---

            var response = new AuthResponseDto
            {
                Message = "User created successfully!",
                UserInfo = new UserInfoDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email
                },
                AccessToken = accessToken,
                AccessTokenExpiry = _tokenService.GenerateAccessTokenExpiry()
            };

            return Ok(response);
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return BadRequest(ModelState);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password,
            model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Логически это не должно произойти, если вход успешен
                return Unauthorized(new { error = "Invalid login attempt." });
            }

            // --- НОВОЕ: Генерация и сохранение Refresh токена ---
            var (accessToken, refreshToken) = await GenerateTokensAndSetCookieAsync(user);
            // --- КОНЕЦ НОВОГО ---

            var response = new AuthResponseDto
            {
                Message = "Login successful!",
                UserInfo = new UserInfoDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email
                },
                AccessToken = accessToken,
                AccessTokenExpiry = _tokenService.GenerateAccessTokenExpiry()
            };

            return Ok(response);
        }

        return Unauthorized(new { error = "Invalid login attempt." });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        // 1. Получить Refresh токен из HttpOnly cookie
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new { error = "Refresh token is required." });
        }

        // 2. Найти Refresh токен в БД
        var tokenEntity = await _context.RefreshTokens
            .Include(rt => rt.User) // Подгружаем связанного пользователя
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (tokenEntity == null || !tokenEntity.IsActive)
        {
            // Токен не найден или неактивен (истёк или отозван)
            return Unauthorized(new { error = "Invalid refresh token." });
        }

        var user = tokenEntity.User;

        // 3. Проверить, что пользователь существует
        if (user == null)
        {
            return Unauthorized(new { error = "Invalid refresh token." });
        }

        // 4. Отозвать старый Refresh токен (rotation)
        tokenEntity.RevokedAt = DateTime.UtcNow;
        tokenEntity.RevokedByIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
        // tokenEntity.ReplacedByToken = ... будет установлено при создании нового токена

        // 5. Сгенерировать новые токены
        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenExpiry = _tokenService.GenerateRefreshTokenExpiry();

        // 6. Сохранить новый Refresh токен в БД, связав с пользователем
        var newTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            ExpiresAt = newRefreshTokenExpiry,
            // CreatedAt автоматически будет установлен в DateTime.UtcNow
            // RevokedAt = null (по умолчанию)
            // RevokedByIp = null (по умолчанию)
            // ReplacedByToken = null (по умолчанию)
        };

        // Устанавливаем ReplacedByToken для старого токена
        tokenEntity.ReplacedByToken = newRefreshToken;

        _context.RefreshTokens.Update(tokenEntity); // Обновляем старый токен
        _context.RefreshTokens.Add(newTokenEntity); // Добавляем новый токен
        await _context.SaveChangesAsync(); // Сохраняем оба изменения

        // 7. Установить новый Refresh токен в cookie
        SetRefreshTokenCookie(newRefreshToken, newRefreshTokenExpiry);

        // 8. Вернуть новый Access токен в теле ответа
        var response = new AuthResponseDto
        {
            Message = "Tokens refreshed successfully!",
            UserInfo = new UserInfoDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            },
            AccessToken = newAccessToken,
            AccessTokenExpiry = _tokenService.GenerateAccessTokenExpiry() // Новое время истечения для нового Access токена
        };

        return Ok(response);
    }
    
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // 1. Получить Refresh токен из HttpOnly cookie
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            // Если cookie нет, можно просто вернуть Ok, так как сессия уже "закончена"
            // Или вернуть Unauthorized, если логика требует обязательного presence токена
            // Для простоты и безопасности часто возвращают Ok.
            return Ok(new { message = "Logged out successfully (no refresh token found in cookie)." });
        }

        // 2. Найти Refresh токен в БД
        var tokenEntity = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (tokenEntity != null)
        {
            // 3. Отозвать Refresh токен (установить время отзыва)
            tokenEntity.RevokedAt = DateTime.UtcNow;
            tokenEntity.RevokedByIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

            _context.RefreshTokens.Update(tokenEntity);
            await _context.SaveChangesAsync();
        }
        // Если токен не найден в БД, но был в cookie, это странно, но мы всё равно очищаем cookie клиента.

        // 4. Установить "просроченную" cookie с тем же именем, чтобы удалить её в браузере клиента
        var expiredCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = HttpContext.Request.Scheme == "https", // Используем ту же логику, что и для установки
            SameSite = SameSiteMode.None, // Используем ту же логику, что и для установки
            Expires = DateTime.UtcNow.AddDays(-1), // Установить время в прошлом
            Path = "/" // Убедитесь, что путь правильный
        };

        Response.Cookies.Append("refreshToken", "", expiredCookieOptions);

        return Ok(new { message = "Logged out successfully." });
    }

    // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---
    private async Task<(string accessToken, string refreshToken)> GenerateTokensAndSetCookieAsync(User user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiry = _tokenService.GenerateRefreshTokenExpiry();

        // Сохранить Refresh токен в БД
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = refreshTokenExpiry,
            // CreatedAt автоматически будет установлен в DateTime.UtcNow
        };
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync(); // Сохраняем в БД

        // Установить Refresh токен в HttpOnly cookie
        SetRefreshTokenCookie(refreshToken, refreshTokenExpiry);

        return (accessToken, refreshToken);
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime expiresAt)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // true для HTTPS в продакшене, false для локальной разработки HTTP
            SameSite = SameSiteMode.None, // Важно для CORS
            Expires = expiresAt,
            Path = "/" // Убедитесь, что путь правильный
        };

        // Для локальной разработки (HTTP) установите Secure = false
        if (HttpContext.Request.Scheme == "http")
        {
            cookieOptions.Secure = false;
            // Для HTTP в разработке может потребоваться SameSite = Lax или None, в зависимости от настроек браузера
            // SameSiteMode.None часто требует Secure=true, поэтому в разработке может быть Lax
            // cookieOptions.SameSite = SameSiteMode.Lax; // Альтернатива для разработки
        }

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
    // --- КОНЕЦ ВСПОМОГАТЕЛЬНЫХ МЕТОДОВ ---
}