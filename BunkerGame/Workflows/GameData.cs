namespace BunkerGame.Workflows;

public class GameData
{
    public Guid GameId { get; set; }
    public bool IsGameOver { get; set; } = false;

    // --- Настройки игры ---
    public int PlayersCount { get; set; }
    public int BunkerSpots { get; set; } // Мест в бункере (N)
    public int VotesRequiredPerPlayer { get; set; } // (N-1)
    
    public List<Guid> TurnOrder { get; set; } = new();
    
    // --- Текущее состояние ---
    public List<StageConfig> Stages { get; set; } = new();
    public int CurrentStageIndex { get; set; } = 0;
    
    // Индекс текущего раунда ВНУТРИ этапа (0, 1, 2)
    public int RoundsPlayedInCurrentStage { get; set; } = 0; 

    // Индекс текущего игрока, который ходит (для цикла хода)
    public int CurrentPlayerTurnIndex { get; set; } = 0;
    
    // Буфер голосов текущего раунда: КтоГолосовал -> ЗаКого(Список)
    public Dictionary<Guid, List<Guid>> CurrentRoundVotes { get; set; } = new();
}

public class StageConfig
{
    public string Name { get; set; } = string.Empty;
    public int RoundsCount { get; set; } // 3, 2, 1
    public int VoteWeight { get; set; }  // 1, 2, 3
}