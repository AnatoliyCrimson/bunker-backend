using System.Security.Claims;
using BunkerGame.DTOs.Game;
using BunkerGame.Services.GameService;
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
            await _gameService.PresentationTrait(userId, dto.TraitCode);
            return Ok(new { message = "Характеристика открыта" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    /// <summary>
    /// Завершить обсуждение (только для хоста)
    /// </summary>
    [HttpPost("end-discussion")]
    public async Task<IActionResult> EndDiscussion(Guid gameId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            await _gameService.EndDiscussion(userId);
            return Ok(new { message = "Обсуждение закончено, начато голосование" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    /// <summary>
    /// Завершить фазу истории и начать игру (только для хоста)
    /// </summary>
    [HttpPost("end-story")]
    public async Task<IActionResult> EndStory()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            await _gameService.EndStoryPhaseAsync(userId);
            return Ok(new { message = "Фаза истории завершена, начата игра" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Проголосовать за игроков (в фазе голосования)
    /// </summary>
    [HttpPost("vote")]
    public async Task<IActionResult> Vote([FromBody] VoteDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            await _gameService.SubmitVote(userId, dto.TargetsPlayerId);
            return Ok(new { message = "Vote accepted" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}