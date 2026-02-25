using BunkerGame.Data;
using BunkerGame.Models;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;

namespace BunkerGame.Services;

public class GameService : IGameService
{
    private readonly ApplicationDbContext _context;
    public GameService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Guid> StartGameAsync(Guid roomId, Guid userId, int additionalRounds)
    {
        var room = await _context.Rooms
            .Include(r => r.Players)
            .FirstOrDefaultAsync(r => r.Id == roomId);
        
        if (room == null) throw new Exception("Room not found");
        if (room.HostId != userId) throw new InvalidOperationException("Запускать игру может только хост.");
        
        var game = new Game
        {
            Id = Guid.NewGuid(),
            HostId = userId,
            Phase = "Init",
            RoomId = roomId,
            StartedAt = DateTime.UtcNow,
            AdditionalRounds = additionalRounds
        };
        _context.Games.Add(game);
        
        
    }
}