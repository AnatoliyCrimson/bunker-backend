using BunkerGame.DTOs.Room;
using BunkerGame.Models;

namespace BunkerGame.Services;

public interface IRoomService
{
    Task<Room> CreateRoomAsync(Guid hostId, string name);
    Task<List<RoomDto>> GetActiveRoomsAsync();
    Task<bool> JoinRoomAsync(Guid roomId, Guid userId);
    Task<Room?> GetRoomAsync(Guid roomId);
}