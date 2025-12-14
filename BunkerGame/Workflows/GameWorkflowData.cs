namespace BunkerGame.Workflows;

public class GameWorkflowData
{
    public Guid GameId { get; set; }

    // Текущий уровень этапа (1, 2, 3...)
    // Нужен для определения веса голоса (1 балл, 2 балла...) 
    // и логики переключения раундов.
    public int StageLevel { get; set; } = 1;

    // Сколько раундов осталось сыграть в текущем этапе.
    // (Например, в 1-м этапе здесь будет 3, затем уменьшается до 0)
    public int RoundsLeftInCurrentStage { get; set; }

    // Список ID игроков, которые уже проголосовали в текущей фазе голосования.
    // Будем использовать это для проверки: "Все ли проголосовали?"
    // После каждого голосования этот список будем очищать.
    public List<Guid> VotedPlayerIds { get; set; } = new();
    
    public List<Guid> PlayerIds { get; set; } = new(); 
    
    public Dictionary<Guid, Guid> CurrentVotes { get; set; } = new();
    
    public int AdditionalRoundsConfig { get; set; } 
}