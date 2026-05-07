namespace BunkerGame.Models.GameModels;

public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Фаза игры
    /// </summary>
    public GamePhase Phase { get; set; } = GamePhase.Init;
    
    /// <summary>
    /// Текущий этап
    /// </summary>
    public int CurrentStage { get; set; } = 1;
    
    /// <summary>
    /// Текущий раунд
    /// </summary>
    public int CurrentRoundNumber { get; set; } = 0;
    
    /// <summary>
    /// Количество дополнительных раундов
    /// </summary>
    public int AdditionalRounds { get; set; } = 0;
    
    /// <summary>
    /// Мест в бункере
    /// </summary>
    public int AvailablePlaces { get; set; } 
    
    /// <summary>
    /// Дата и время окончания обсуждения 
    /// </summary>
    public DateTime? DiscussionEndsAt { get; set; }
    
    /// <summary>
    /// Текущие голоса игроков
    /// </summary>
    public Dictionary<Guid, List<Guid>> CurrentVotes { get; set; } = new();
    
    /// <summary>
    /// Дата и время начала игры
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Ид хоста
    /// </summary>
    public Guid HostId { get; set; }
    
    /// <summary>
    /// Ид текущего игрока, который открывает характеристику
    /// </summary>
    public Guid? CurrentTurnPlayerId { get; set; } 
    
    /// <summary>
    /// Ид комнаты, к которой привязана игра
    /// </summary>
    public Guid RoomId { get; set; } // к какой комнате привязано
    public Room? Room { get; set; }
    
    /// <summary>
    /// Название катастрофы
    /// </summary>
    public string? DisasterName { get; set; }
    
    /// <summary>
    /// Описание катастрофы (генерация ИИ)
    /// </summary>
    public string? DisasterDescription { get; set; }

    /// <summary>
    /// Описание бункера (генерация ИИ)
    /// </summary>
    public string? BunkerDescription { get; set; }

    /// <summary>
    /// Список комнат бункера в формате JSON (генерация ИИ)
    /// </summary>
    public List<BunkerRoom>? BunkerRooms { get; set; }

    /// <summary>
    /// Финальный вердикт выживания (генерация ИИ в конце игры)
    /// </summary>
    public string? AchievementVerdict { get; set; }
    
    /// <summary>
    /// Список игроков
    /// </summary>
    public ICollection<Player> Players { get; set; } = new List<Player>();
}