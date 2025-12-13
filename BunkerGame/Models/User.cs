using Microsoft.AspNetCore.Identity;

namespace BunkerGame.Models;

public class User : IdentityUser<Guid>
{
    public User()
    {
        Id = Guid.NewGuid();
    }

    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? AvatarUrl { get; set; }
    
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    // --- ИЗМЕНЕНИЯ: Текущее состояние (State) ---

    // 1. Связь с текущей комнатой (Lobby)
    // Если null - пользователь не в комнате
    public Guid? CurrentRoomId { get; set; }
    public Room? CurrentRoom { get; set; }

    // 2. Связь с текущей игрой (Game process)
    // Если null - пользователь не в активной игре
    public Guid? CurrentGameId { get; set; }
    public Game? CurrentGame { get; set; }
    
    // 3. Связь с персонажем в текущей игре (вместо списка PlayerSessions)
    // Т.к. мы храним только текущее состояние, связь 1 к 1 (или 0 к 1)
    public Player? CurrentPlayerCharacter { get; set; }
}