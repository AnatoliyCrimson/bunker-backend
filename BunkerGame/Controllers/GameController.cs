using System.Security.Claims;
using BunkerGame.DTOs.Game;
using BunkerGame.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BunkerGame.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;

    public GameController(IGameService gameService)
    {
        _gameService = gameService;
    }

    /// <summary>
    /// Начать игру (только для хоста комнаты)
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartGame([FromBody] StartGameDto dto)
    {
        try 
        {
            // Метод сервиса сам проверит существование комнаты и удалит её после старта
            var gameId = await _gameService.StartGameAsync(dto.RoomId);
            return Ok(new { gameId, message = "Game started successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить текущее состояние игры (Игроки, их карты, чей ход)
    /// Используется для начальной загрузки или поллинга
    /// </summary>
    [HttpGet("{gameId}/state")]
    public async Task<IActionResult> GetState(Guid gameId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var state = await _gameService.GetGameStateForUserAsync(gameId, userId);
        
        if (state == null) return NotFound("Game not found.");
        
        return Ok(state);
    }
}