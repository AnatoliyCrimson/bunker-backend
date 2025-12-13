namespace BunkerGame.Models;

public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public ICollection<Player> Players { get; set; } = new List<Player>();

    public string? CurrentStep { get; set; }

    public Guid? WorkflowInstanceId { get; set; } 

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}