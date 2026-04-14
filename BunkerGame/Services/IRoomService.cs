using BunkerGame.DTOs.Room;
using BunkerGame.Models;

namespace BunkerGame.Services;

public interface IRoomService
{
    Task<Room> CreateRoomAsync(Guid hostId);
    Task<List<RoomDto>> GetActiveRoomsAsync();
    Task<RoomDetailsDto?> GetRoomDetailsAsync(Guid roomId);
    Task<RoomDetailsDto?> GetRoomStateAsync(Guid userId);
    Task<Guid?> JoinRoomAsync(string inviteCode, Guid userId);
    
    Task<bool> LeaveRoomAsync(Guid userId);
    Task<bool> KickPlayerAsync(Guid hostId, Guid targetPlayerId);
    Task<bool> DeleteRoomHostAsync(Guid userId);
    Task<bool> DeleteRoomAsync(Guid roomId, Guid userId);
}