namespace BunkerGame.Models;

public class AiPrompt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Уникальный ключ промпта (например, "game_init_story")
    /// </summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// Текст системного промпта
    /// </summary>
    public string SystemPrompt { get; set; } = null!;

    /// <summary>
    /// Текст пользовательского промпта (шаблон)
    /// </summary>
    public string UserPromptTemplate { get; set; } = null!;

    /// <summary>
    /// Температура генерации (0.0 - 1.0)
    /// </summary>
    public float Temperature { get; set; } = 0.6f;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}