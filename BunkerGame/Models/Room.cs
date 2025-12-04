namespace BunkerGame.Models;

public class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string InviteCode { get; set; } = string.Empty;

    public Guid HostId { get; set; }

    public List<Guid> PlayerIds { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}