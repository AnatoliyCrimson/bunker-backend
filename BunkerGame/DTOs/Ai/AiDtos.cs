namespace BunkerGame.DTOs.Ai;

/// <summary>
/// Предыстория, характеристики игроков
/// </summary>
public class AiStoryResponse
{
    public string DisasterName { get; set; } = null!;
    public string DisasterDescription { get; set; } = null!;
    public string BunkerDescription { get; set; } = null!;
    public List<BunkerRoom> Rooms { get; set; } = new();
    public List<AiPlayerSetup> Players { get; set; } = new();
}

public class AiPlayerSetup
{
    public string Profession { get; set; } = null!;
    public string Health { get; set; } = null!;
    public string Phobia { get; set; } = null!;
    public string Bio { get; set; } = null!;
    public string Inventory { get; set; } = null!;
    public string Hobby { get; set; } = null!;
    public string Knowledge { get; set; } = null!;
    public string Character { get; set; } = null!;
    public string Info { get; set; } = null!;
}

public class BunkerRoom
{
    public string Name { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Description { get; set; } = null!;
}

public class AiVerdictResponse
{
    public int Score { get; set; }
    public string ImpactAnalysis { get; set; } = null!;
    public List<TimelineEvent> SurvivalTimeline { get; set; } = new();
    public AlternativeOutcome BestAlternative { get; set; } = null!;
    public AlternativeOutcome WorstAlternative { get; set; } = null!;
}

public class AlternativeOutcome
{
    public string Description { get; set; } = null!;
    public int Score { get; set; }
}

public class TimelineEvent
{
    public string Period { get; set; } = null!; // "1 год", "5 лет", "10 лет"
    public string Event { get; set; } = null!;
}