using BunkerGame.DTOs.Room;
using BunkerGame.Models;

namespace BunkerGame.Services;

public class RoomService : IRoomService
{
    public Task<Room> CreateRoomAsync(Guid hostId, string name)
    {
        throw new NotImplementedException();
    }

    public Task<List<RoomDto>> GetActiveRoomsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> JoinRoomAsync(Guid roomId, Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<Room?> GetRoomAsync(Guid roomId)
    {
        throw new NotImplementedException();
    }
}