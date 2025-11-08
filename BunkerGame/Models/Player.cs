using System.ComponentModel.DataAnnotations;

namespace BunkerGame.Models;

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Ссылка на пользователя
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    // Ссылка на игру
    public Guid GameId { get; set; }
    public Game? Game { get; set; }
    
    // Характеристики игрока в контексте конкретной игры
    public string Profession { get; set; } = string.Empty;       // Профессия
    public string Health { get; set; } = string.Empty;           // Здоровье
    public string Age { get; set; } = string.Empty;              // Возраст
    public string Gender { get; set; } = string.Empty;           // Пол
    public string CharacterTrait { get; set; } = string.Empty;   // Черта характера
    public string Hobby { get; set; } = string.Empty;            // Хобби
    public string Phobia { get; set; } = string.Empty;           // Фобия
    public string AdditionalInfo { get; set; } = string.Empty;   // Дополнительная информация
    
    // Дополнительные игровые данные
    public int VoteCount { get; set; } = 0;
    public bool IsReady { get; set; } = false;
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}