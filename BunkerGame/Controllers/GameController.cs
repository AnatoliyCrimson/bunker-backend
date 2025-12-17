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
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try 
        {
            var gameId = await _gameService.StartGameAsync(dto.RoomId, userId);
            return Ok(new { gameId });
        }
        catch (InvalidOperationException ex) // Ловим нашу ошибку прав
        {
            return StatusCode(403, new { message = ex.Message });
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
    
    /// <summary>
    /// Получить список всех активных игр (только краткая инфо)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllGames()
    {
        var games = await _gameService.GetAllGamesAsync();
        return Ok(games);
    }

    /// <summary>
    /// Удалить игру по ID (Админ или Хост)
    /// </summary>
    [HttpDelete("{gameId}")]
    public async Task<IActionResult> DeleteGame(Guid gameId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            var success = await _gameService.DeleteGameAsync(gameId, userId);
            if (!success) return NotFound("Game not found");
            return Ok(new { message = "Game deleted successfully" });

        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        
        

    }
}