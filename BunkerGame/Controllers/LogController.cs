using BunkerGame.Data;
using BunkerGame.Models.GameModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BunkerGame.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LogsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Получить все логи событий всех игр
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GameEventLog>>> GetAllLogs()
    {
        return await _context.GameEventLogs
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Получить логи конкретной комнаты по её ID
    /// </summary>
    [HttpGet("room/{roomId}")]
    public async Task<ActionResult<IEnumerable<GameEventLog>>> GetLogsByRoomId(Guid roomId)
    {
        // Находим игру, привязанную к этой комнате
        var game = await _context.Games.FirstOrDefaultAsync(g => g.RoomId == roomId);
        
        if (game == null)
        {
            return NotFound("Для этой комнаты игра не найдена или еще не создана.");
        }

        return await _context.GameEventLogs
            .Where(l => l.GameId == game.Id)
            .OrderBy(l => l.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Получить логи конкретной игры по её ID
    /// </summary>
    [HttpGet("game/{gameId}")]
    public async Task<ActionResult<IEnumerable<GameEventLog>>> GetLogsByGameId(Guid gameId)
    {
        var logs = await _context.GameEventLogs
            .Where(l => l.GameId == gameId)
            .OrderBy(l => l.Timestamp)
            .ToListAsync();

        if (!logs.Any())
        {
            // Проверим, существует ли игра вообще
            var gameExists = await _context.Games.AnyAsync(g => g.Id == gameId);
            if (!gameExists) return NotFound("Игра не найдена.");
        }

        return logs;
    }
}
