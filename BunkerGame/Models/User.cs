using Microsoft.AspNetCore.Identity;

namespace BunkerGame.Models;

public class User : IdentityUser<Guid> // используем Guid вместо string для Id
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Навигационное свойство для участия в играх
    public List<Player> PlayerSessions { get; set; } = new();
    
    // IdentityUser уже содержит:
    //   public Guid Id { get; set; }
    //   public string Email { get; set; }
    //   public string PasswordHash { get; set; } — пароль хранится в хешированном виде!
    //   public string UserName { get; set; } — обычно = Email

}