namespace BunkerGame.Workflows;

public class GameData
{
    public Guid GameId { get; set; }
    public bool IsGameOver { get; set; } = false;

    public int PlayersCount { get; set; }
    public int BunkerSpots { get; set; } 
    public int VotesRequiredPerPlayer { get; set; }
    
    // Инициализируем сразу!
    public List<Guid> TurnOrder { get; set; } = new();
    
    // Инициализируем сразу!
    public List<StageConfig> Stages { get; set; } = new();
    
    public int CurrentStageIndex { get; set; } = 0;
    public int RoundsPlayedInCurrentStage { get; set; } = 0; 
    public int CurrentPlayerTurnIndex { get; set; } = 0;
    
    // Инициализируем сразу!
    public Dictionary<Guid, List<Guid>> CurrentRoundVotes { get; set; } = new();
}

public class StageConfig
{
    public string Name { get; set; } = string.Empty;
    public int RoundsCount { get; set; } 
    public int VoteWeight { get; set; }  
}