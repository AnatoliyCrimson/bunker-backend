using BunkerGame.Models;
using System.Security.Claims;
using BunkerGame.DTOs.Avatar;
using BunkerGame.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BunkerGame.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Доступ только для авторизованных пользователей
public class UsersController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly IFileStorageService _fileStorage;

    public UsersController(UserManager<User> userManager, IFileStorageService fileStorage)
    {
        _userManager = userManager;
        _fileStorage = fileStorage;
    }
    
    /// <summary>
    /// Загрузить или обновить аватарку текущего пользователя
    /// </summary>
    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar([FromForm] UploadAvatarDto dto)
    {
        // 1. Валидация входных данных
        if (dto.File == null || dto.File.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        // Ограничение размера (например, 5 МБ)
        const long maxFileSize = 5 * 1024 * 1024;
        if (dto.File.Length > maxFileSize)
        {
            return BadRequest("File size exceeds 5 MB.");
        }

        // 2. Получаем текущего пользователя
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");

        try
        {
            // 3. Если старая аватарка есть — удаляем файл
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                await _fileStorage.DeleteFileAsync(user.AvatarUrl);
            }

            // 4. Сохраняем новый файл
            var newAvatarUrl = await _fileStorage.SaveFileAsync(dto.File, "avatars");

            // 5. Обновляем пользователя в БД
            user.AvatarUrl = newAvatarUrl;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // 6. Возвращаем новый URL
            return Ok(new { url = newAvatarUrl });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message); // Ошибка валидации расширения файла
        }
        catch (Exception ex)
        {
            // Логируем ошибку реально, здесь для примера вывод в ответ
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    
    /// <summary>
    /// Получить список всех пользователей (для отладки/админки)
    /// </summary>
    [HttpGet]
    public IActionResult GetUsers()
    {
        var users = _userManager.Users.Select(u => new
        {
            u.Id,
            u.UserName,
            u.Email,
            u.CreatedAt
        }).ToList();

        return Ok(users);
    }

    /// <summary>
    /// Получить данные конкретного пользователя по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound("User not found");

        var result = new
        {
            user.Id,
            UserName = user.UserName,
            user.Email,
            user.CreatedAt
        };
        return Ok(result);
    }

    /// <summary>
    /// Удалить пользователя по ID (НОВОЕ)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        
        if (user == null)
        {
            return NotFound("User not found");
        }

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            // Возвращаем ошибки, если удаление не удалось (например, системные ограничения)
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "User deleted successfully" });
    }
}