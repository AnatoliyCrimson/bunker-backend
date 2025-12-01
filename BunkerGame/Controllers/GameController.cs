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
            var gameId = await _gameService.StartGameAsync(dto.RoomId);
            return Ok(new { gameId });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Получить текущее состояние игры (поллинг или начальная загрузка)
    /// Данные фильтруются сервисом (Fog of War)
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