namespace BunkerGame.Models;

public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid HostId { get; set; }
    
    public ICollection<Player> Players { get; set; } = new List<Player>();
    
    public string Phase { get; set; } = "Initialization";
    
    public int CurrentRoundNumber { get; set; } = 1;
    
    public int AdditionalRounds { get; set; } = 0;

    public int AvailablePlaces { get; set; }
    
    public Guid? CurrentTurnPlayerId { get; set; }
    
    public Guid? WorkflowInstanceId { get; set; } 

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}