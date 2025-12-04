using BunkerGame.Data;
using BunkerGame.DTOs.Room;
using BunkerGame.Models;
using Microsoft.EntityFrameworkCore;

namespace BunkerGame.Services;

public class RoomService : IRoomService
{
    private readonly ApplicationDbContext _context;
    private const string AllowChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";

    public RoomService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Room> CreateRoomAsync(Guid hostId)
    {
        string inviteCode;
        bool exists;
        
        do
        {
            inviteCode = GenerateRandomCode(6);
            // Проверяем в БД, есть ли уже такой код
            exists = await _context.Rooms.AnyAsync(r => r.InviteCode == inviteCode);
        } 
        while (exists);
        
        var room = new Room
        {
            HostId = hostId,
            InviteCode = inviteCode,
            PlayerIds = new List<Guid> { hostId } // Хост сразу добавляется в список
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
        return room;
    }

    private static string GenerateRandomCode(int length)
    {
        return new string(Enumerable.Repeat(AllowChars, length)
            .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }
    
    public async Task<List<RoomDto>> GetActiveRoomsAsync()
    {
        // Возвращаем список комнат, сортируя по новизне
        return await _context.Rooms
            .Select(r => new RoomDto
            {
                Id = r.Id,
                InviteCode = r.InviteCode,
                HostId = r.HostId,
                PlayerCount = r.PlayerIds.Count,
                CreatedAt = r.CreatedAt
            })
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
    
    // --- НОВЫЙ МЕТОД ---
    public async Task<RoomDetailsDto?> GetRoomDetailsAsync(Guid roomId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) return null;

        // Находим пользователей, чьи ID есть в списке room.PlayerIds
        // Используем UserName как имя игрока
        var players = await _context.Users
            .Where(u => room.PlayerIds.Contains(u.Id))
            .Select(u => new RoomPlayerDto
            {
                Id = u.Id,
                Name = u.UserName ?? "Unknown" 
            })
            .ToListAsync();

        return new RoomDetailsDto
        {
            Id = room.Id,
            InviteCode  = room.InviteCode ,
            HostId = room.HostId,
            CreatedAt = room.CreatedAt,
            Players = players
        };
    }
    // -------------------

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
    
    // --- НОВЫЙ МЕТОД ---
    public async Task<bool> RemovePlayerAsync(Guid roomId, Guid playerId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) return false;

        // Проверяем, есть ли такой игрок в комнате
        if (!room.PlayerIds.Contains(playerId)) return false;

        room.PlayerIds.Remove(playerId);
        
        await _context.SaveChangesAsync();
        return true;
    }
    // -------------------

    public async Task<Room?> GetRoomAsync(Guid roomId)
    {
        return await _context.Rooms.FindAsync(roomId);
    }
}