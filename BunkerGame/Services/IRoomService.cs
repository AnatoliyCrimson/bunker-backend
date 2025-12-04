using BunkerGame.DTOs.Room;
using BunkerGame.Models;

namespace BunkerGame.Services;

public interface IRoomService
{
    Task<Room> CreateRoomAsync(Guid hostId);
    Task<List<RoomDto>> GetActiveRoomsAsync();
    Task<RoomDetailsDto?> GetRoomDetailsAsync(Guid roomId);
    Task<bool> JoinRoomAsync(Guid roomId, Guid userId);
    
    // --- НОВОЕ ---
    Task<bool> RemovePlayerAsync(Guid roomId, Guid playerId);
    // -------------
    
    Task<Room?> GetRoomAsync(Guid roomId);
}