using Microsoft.AspNetCore.Identity;

namespace BunkerGame.Models;

public class User : IdentityUser<Guid>
{
    public User()
    {
        Id = Guid.NewGuid(); // Инициализируем уникальный идентификатор
    }

    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Навигационное свойство для участия в играх
    public List<Player> PlayerSessions { get; set; } = new();
}