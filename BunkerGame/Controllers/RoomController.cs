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
        var room = await _roomService.CreateRoomAsync(userId);
        
        return Ok(new { roomId = room.Id, inviteCode = room.InviteCode });
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
    /// Присоединить пользователя к комнате (по ID из запроса)
    /// </summary>
    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinRoomDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        var roomId = await _roomService.JoinRoomAsync(dto.InviteCode.ToUpper(), userId);
        
        if (roomId == null)
        {
            return BadRequest("Unable to join: Room not found, full, or closed.");
        }
        
        return Ok(new { message = "Joined successfully", roomId = roomId });
    }
    
    /// <summary>
    /// Самостоятельный выход игрока из комнаты
    /// </summary>
    [HttpPost("leave")]
    public async Task<IActionResult> Leave([FromBody] LeaveRoomDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        var success = await _roomService.RemovePlayerAsync(dto.RoomId, userId);

        if (!success)
        {
            return BadRequest("Unable to leave: Player not found in this room.");
        }
        
        return Ok(new { message = "Left successfully" });
    }
    
    /// <summary>
    /// Исключить игрока из комнаты (Только Хост)
    /// </summary>
    [HttpPost("kick")]
    public async Task<IActionResult> KickPlayer([FromBody] KickPlayerDto dto)
    {
        // 1. Получаем ID того, кто делает запрос (Текущий пользователь)
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // 2. Загружаем комнату, чтобы проверить владельца
        var room = await _roomService.GetRoomAsync(dto.RoomId);
        if (room == null)
        {
            return NotFound("Room not found");
        }

        // 3. ПРОВЕРКА НА ХОСТА
        if (room.HostId != currentUserId)
        {
            return StatusCode(403, "Only the host can kick players."); // 403 Forbidden
        }

        // 4. Удаляем игрока
        var success = await _roomService.RemovePlayerAsync(dto.RoomId, dto.UserId);
        
        if (!success)
        {
            return BadRequest("Player not found in this room.");
        }

        return Ok(new { message = "Player kicked successfully" });
    }
    
    /// <summary>
    /// Удалить комнату по ID (НОВОЕ)
    /// </summary>
    [HttpDelete("{id}")]
    // [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRoom(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        var room = await _roomService.GetRoomAsync(id);
        
        if (room == null)
        {
            return NotFound("Room not found");
        }
        
        if (room.HostId != userId)
        {
            return StatusCode(403, "Only the host can delete the room.");
        }
        
        var success = await _roomService.DeleteRoomAsync(id);

        if (!success)
        {
            return NotFound("Room not found");
        }

        return Ok(new { message = "Room deleted successfully" });
    }
}