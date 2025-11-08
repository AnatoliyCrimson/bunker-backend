namespace BunkerGame.Models;

public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RoomId { get; set; }

    public List<Guid> PlayerIds { get; set; } = new();
    
    public List<Player> Players { get; set; } = new();

    public string? CurrentStep { get; set; }

    public Guid? WorkflowInstanceId { get; set; } 

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}