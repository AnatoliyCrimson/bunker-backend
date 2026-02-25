namespace BunkerGame.Models;

public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HostId { get; set; }
    public string Phase { get; set; } = "Init";
    public int CurrentRoundNumber { get; set; } = 0;
    public int AdditionalRounds { get; set; } = 0;
    public Guid? CurrentTurnPlayerId { get; set; } // кто сейчас открывает характеристику
    public int AvailablePlaces { get; set; } // мест в бункере
    public ICollection<Player> Players { get; set; } = new List<Player>();
    public Guid RoomId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}