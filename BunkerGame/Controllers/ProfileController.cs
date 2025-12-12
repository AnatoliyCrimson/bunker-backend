using BunkerGame.Models;
using System.Security.Claims;
using BunkerGame.DTOs.Avatar;
using BunkerGame.DTOs.Profile;
using BunkerGame.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BunkerGame.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Доступ только для авторизованных пользователей
public class ProfileController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly IFileStorageService _fileStorage;

    public ProfileController(UserManager<User> userManager, IFileStorageService fileStorage)
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
    /// Изменить имя текущего пользователя
    /// </summary>
    [HttpPut("name")]
    public async Task<IActionResult> ChangeName([FromBody] ChangeNameDto dto)
    { 
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");
        
        user.Name = dto.NewName;
        
        var result = await _userManager.UpdateAsync(user);
        
        if (!result.Succeeded) return BadRequest(result.Errors);
        
        return Ok(new { message = "Name updated successfully", newName = user.Name});
    }
    
    [HttpPut("email")]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");

        var userWithSameEmail = await _userManager.FindByEmailAsync(dto.NewEmail);
        if (userWithSameEmail != null && userWithSameEmail.Id != user.Id)
        {
            return BadRequest("This email is already taken.");
        }

        user.Email = dto.NewEmail;
        user.UserName = dto.NewEmail;
        
        await _userManager.UpdateNormalizedEmailAsync(user);
        await _userManager.UpdateNormalizedUserNameAsync(user);

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Email updated successfully", newEmail = user.Email });
    }
    
    /// <summary>
    /// Сменить пароль
    /// </summary>
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Password changed successfully" });
    }
    
    /// <summary>
    /// Проверить текущий пароль (без смены)
    /// </summary>
    [HttpPost("check-password")]
    public async Task<IActionResult> CheckPassword([FromBody] CheckPasswordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");

        bool isCorrect = await _userManager.CheckPasswordAsync(user, dto.Password);

        if (!isCorrect)
        {
            return BadRequest("Неверный текущий пароль.");
        }

        return Ok(new { message = "Password is correct" });
    }
}