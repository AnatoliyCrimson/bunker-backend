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
    public async Task<IActionResult> Create([FromBody] CreateRoomDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var room = await _roomService.CreateRoomAsync(userId, dto.Name);
        
        return Ok(new { roomId = room.Id, name = room.Name });
    }

    /// <summary>
    /// Получить список всех комнат (ИСПРАВЛЕНО: убран "list" из пути)
    /// </summary>
    [HttpGet] 
    public async Task<IActionResult> GetList()
    {
        var rooms = await _roomService.GetActiveRoomsAsync();
        return Ok(rooms);
    }

    /// <summary>
    /// Получить информацию о конкретной комнате и игроках в ней (НОВОЕ)
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
    /// Присоединиться к комнате
    /// </summary>
    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinRoomDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _roomService.JoinRoomAsync(dto.RoomId, userId);
        
        if (!success) return BadRequest("Unable to join room (full or not found).");
        
        return Ok(new { message = "Joined successfully" });
    }
}