namespace BunkerGame.Models;

public class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string InviteCode { get; set; } = string.Empty;

    public Guid HostId { get; set; } // ID создателя (для прав управления)
    
    public ICollection<User> Players { get; set; } = new List<User>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid GameId { get; set; }
    
    public bool IsGameStart { get; set; } = false;
}