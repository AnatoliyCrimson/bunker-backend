using System.Security.Claims;
using BunkerGame.DTOs.Room;
using BunkerGame.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BunkerGame.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    /// <summary>
    /// Создать новую комнату
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var room = await _roomService.CreateRoomAsync(userId);
            return Ok(new { inviteCode = room.InviteCode });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception e)
        {
            return StatusCode(500, new { message = "An internal error occurred." });
        }
    }

    /// <summary>
    /// Получить список всех комнат
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var rooms = await _roomService.GetActiveRoomsAsync();
        return Ok(rooms);
    }

    /// <summary>
    /// Получить информацию о конкретной комнате и игроках в ней
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoomDetails(Guid id)
    {
        var roomDetails = await _roomService.GetRoomDetailsAsync(id);
        
        if (roomDetails == null)
        {
            return NotFound("Room not found");
        }

        return Ok(roomDetails);
    }
    
    /// <summary>
    /// Получить инфу для фронта
    /// </summary>
    [HttpGet("state")]
    public async Task<IActionResult> GetRoomDetailsState()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var roomDetails = await _roomService.GetRoomStateAsync(userId);
        
        if (roomDetails == null)
        {
            return NotFound("Room not found");
        }

        return Ok(roomDetails);
    }

    /// <summary>
    /// Присоединить пользователя к комнате (по ID из запроса)
    /// </summary>
    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinRoomDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        try
        {
            var roomId = await _roomService.JoinRoomAsync(dto.InviteCode.ToUpper(), userId);
            if (roomId == null)
            {
                return BadRequest("Unable to join: Room not found, full, or closed.");
            }
            
            return Ok(new { message = "Joined successfully", roomId = roomId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception e)
        {
            return StatusCode(500, new { message = "An internal error occurred." });
        }
        
        
        
        
        
    }
    
    /// <summary>
    /// Самостоятельный выход игрока из комнаты
    /// </summary>
    [HttpPost("leave")]
    public async Task<IActionResult> Leave()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        var success = await _roomService.LeaveRoomAsync(userId);

        if (!success)
        {
            return BadRequest("Unable to leave: You are not in a room.");
        }
        
        return Ok(new { message = "Left successfully" });
    }
    
    /// <summary>
    /// Исключить игрока из комнаты (Только Хост)
    /// </summary>
    [HttpPost("kick")]
    public async Task<IActionResult> KickPlayer([FromBody] KickPlayerDto dto)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var success = await _roomService.KickPlayerAsync(currentUserId, dto.UserId);
        
        if (!success)
        {
            return BadRequest("Unable to kick: Room not found, you are not the host, or player not found.");
        }

        return Ok(new { message = "Player kicked successfully" });
    }
    
    /// <summary>
    /// Удалить комнату (Только Хост)
    /// </summary>
    [HttpDelete("host")]
    public async Task<IActionResult> DeleteHostRoom()
    { 
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _roomService.DeleteRoomHostAsync(userId);
        
        if (!result)
        {
            return NotFound("Room not found");
        }

        return Ok(new { message = "Room deleted successfully" });
    }
}