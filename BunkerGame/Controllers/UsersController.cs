using BunkerGame.Models;
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

    public UsersController(UserManager<User> userManager)
    {
        _userManager = userManager;
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