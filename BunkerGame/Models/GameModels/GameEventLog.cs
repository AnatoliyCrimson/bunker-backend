namespace BunkerGame.Models.GameModels;

public class GameEventLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Описание события для ИИ (например: "Игрок Иван открыл Биологию: Мужчина, 30 лет")
    /// </summary>
    public string Description { get; set; } = null!;
}