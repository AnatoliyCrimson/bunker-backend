using BunkerGame.Data;
using BunkerGame.DTOs.Room;
using BunkerGame.Models;
using Microsoft.EntityFrameworkCore;

namespace BunkerGame.Services;

public class RoomService : IRoomService
{
    private readonly ApplicationDbContext _context;

    public RoomService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Room> CreateRoomAsync(Guid hostId, string name)
    {
        var room = new Room
        {
            HostId = hostId,
            Name = name,
            PlayerIds = new List<Guid> { hostId } // Хост сразу добавляется в список
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
        return room;
    }

    public async Task<List<RoomDto>> GetActiveRoomsAsync()
    {
        // Возвращаем список комнат, сортируя по новизне
        return await _context.Rooms
            .Select(r => new RoomDto
            {
                Id = r.Id,
                Name = r.Name,
                HostId = r.HostId,
                PlayerCount = r.PlayerIds.Count,
                CreatedAt = r.CreatedAt
            })
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> JoinRoomAsync(Guid roomId, Guid userId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) return false;

        // Если игрок уже там, возвращаем true
        if (room.PlayerIds.Contains(userId)) return true;

        // Ограничение: максимум 10 игроков (пример)
        if (room.PlayerIds.Count >= 10) return false;

        room.PlayerIds.Add(userId);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Room?> GetRoomAsync(Guid roomId)
    {
        return await _context.Rooms.FindAsync(roomId);
    }
}