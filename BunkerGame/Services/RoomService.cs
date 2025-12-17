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
        var hostUser = await _context.Users.FindAsync(hostId);
        if (hostUser == null) throw new Exception("Host user not found");

        if (hostUser.CurrentRoomId != null)
        {
            throw new InvalidOperationException("You cannot create a room while in an another room.");
        }

        if (hostUser.CurrentGameId != null)
        {
            throw new InvalidOperationException("You cannot create a room while in an active game.");
        }
        
        string inviteCode;
        bool exists;
        do
        {
            inviteCode = GenerateRandomCode(6);
            exists = await _context.Rooms.AnyAsync(r => r.InviteCode == inviteCode);
        } 
        while (exists);
        
        // 4. Создаем комнату
        var room = new Room
        {
            HostId = hostId,
            InviteCode = inviteCode,
        };

        _context.Rooms.Add(room);
        
        hostUser.CurrentRoom = room; 

        await _context.SaveChangesAsync();
        return room;
    }

    public async Task<List<RoomDto>> GetActiveRoomsAsync()
    {
        // Теперь используем Players.Count
        return await _context.Rooms
            .Include(r => r.Players)
            .Select(r => new RoomDto
            {
                Id = r.Id,
                InviteCode = r.InviteCode,
                HostId = r.HostId,
                PlayerCount = r.Players.Count, 
                CreatedAt = r.CreatedAt
            })
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<RoomDetailsDto?> GetRoomDetailsAsync(Guid roomId)
    {
        var room = await _context.Rooms
            .Include(r => r.Players) 
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null) return null;

        var playerDtos = room.Players.Select(u => new RoomPlayerDto
        {
            Id = u.Id,
            Name = u.Name ?? "Unknown", 
            AvatarUrl = u.AvatarUrl
        }).ToList();

        return new RoomDetailsDto
        {
            Id = room.Id,
            InviteCode  = room.InviteCode,
            HostId = room.HostId,
            CreatedAt = room.CreatedAt,
            Players = playerDtos
        };
    }

    public async Task<Guid?> JoinRoomAsync(string inviteCode, Guid userId)
    {
        var room = await _context.Rooms
            .Include(r => r.Players)
            .FirstOrDefaultAsync(r => r.InviteCode == inviteCode);

        if (room == null) return null;

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        if (user.CurrentRoomId == room.Id) return room.Id;

        if (user.CurrentGameId != null)
        {
            throw new InvalidOperationException("You cannot enter in room while in an active game.");
        }
        
        if (user.CurrentRoomId != null) 
        {
            throw new InvalidOperationException("You cannot enter in room while in an another room.");
        }

        if (room.Players.Count >= 10) return null;

        user.CurrentRoomId = room.Id;
        
        await _context.SaveChangesAsync();

        return room.Id;
    }
    
    public async Task<bool> RemovePlayerAsync(Guid roomId, Guid playerId)
    {
        var user = await _context.Users.FindAsync(playerId);
        
        if (user == null || user.CurrentRoomId != roomId) return false;

        user.CurrentRoomId = null;

        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> DeleteRoomAsync(Guid roomId, Guid userId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) return false;
        
        
        if (room.HostId != userId)
        {
            throw new InvalidOperationException("Удалять комнату может только хост.");
        }
        // EF Core благодаря .OnDelete(DeleteBehavior.SetNull) 
        // автоматически проставит user.CurrentRoomId = null для всех участников
        _context.Rooms.Remove(room);
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Room?> GetRoomAsync(Guid roomId)
    {
        return await _context.Rooms.FindAsync(roomId);
    }

    private static string GenerateRandomCode(int length)
    {
        return new string(Enumerable.Repeat(AllowChars, length)
            .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }
}