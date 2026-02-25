using System.Security.Claims;
using BunkerGame.DTOs.Game;
using BunkerGame.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BunkerGame.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlayController : ControllerBase
{
    private readonly IGameService _gameService;

    public PlayController(IGameService gameService)
    {
        _gameService = gameService;
    }

    /// <summary>
    /// Открыть характеристику (в свой ход)
    /// </summary>
    [HttpPost("presentation")]
    public async Task<IActionResult> PresentationTrait([FromBody] PresentationCardDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            await _gameService.RevealTraitAsync(userId, dto.TraitName);
            return Ok(new { message = "Trait revealed" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Проголосовать за игрока (в фазе голосования)
    /// </summary>
    [HttpPost("vote")]
    public async Task<IActionResult> Vote([FromBody] VoteDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            await _gameService.VoteAsync(dto.GameId, userId, dto.TargetPlayerId);
            return Ok(new { message = "Vote accepted" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}