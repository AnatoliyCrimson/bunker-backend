using Microsoft.AspNetCore.Identity;

namespace BunkerGame.Models;

public class User : IdentityUser<Guid>
{
    public User()
    {
        this.Id = Guid.NewGuid(); // Инициализируем уникальный идентификатор
    }

    public string Name { get; set; } = string.Empty;
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string? AvatarUrl { get; set; } 
    
    // Навигационное свойство для участия в играх
    public List<Player> PlayerSessions { get; set; } = new();
}