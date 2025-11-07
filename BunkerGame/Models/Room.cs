namespace BunkerGame.Models;

public class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "Комната #" + Guid.NewGuid().ToString("N")[..6];

    public Guid HostId { get; set; }

    public List<Guid> PlayerIds { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}