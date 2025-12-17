using BunkerGame.DTOs.Room;
using BunkerGame.Models;

namespace BunkerGame.Services;

public interface IRoomService
{
    Task<Room> CreateRoomAsync(Guid hostId);
    Task<List<RoomDto>> GetActiveRoomsAsync();
    Task<RoomDetailsDto?> GetRoomDetailsAsync(Guid roomId);
    Task<Guid?> JoinRoomAsync(string inviteCode, Guid userId);
    
    Task<bool> RemovePlayerAsync(Guid roomId, Guid playerId);
    Task<bool> DeleteRoomAsync(Guid roomId, Guid userId);
    
    Task<Room?> GetRoomAsync(Guid roomId);
}