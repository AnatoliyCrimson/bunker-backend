namespace BunkerGame.Models;

public class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string InviteCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsGameStart { get; set; } = false;
    
    public Guid HostId { get; set; }
    
    public ICollection<User> Users { get; set; } = new List<User>();
    
    public Game? Game { get; set; } 
    
}