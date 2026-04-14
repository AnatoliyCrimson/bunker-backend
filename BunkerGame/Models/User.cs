using System.ComponentModel.DataAnnotations.Schema;
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
    
    public Guid? CurrentRoomId { get; set; }
    public Room? CurrentRoom { get; set; }
    
    public Guid? CurrentGameId { get; set; }
    public Game? CurrentGame { get; set; }
    
    public Player? CurrentPlayerCharacter { get; set; }
}