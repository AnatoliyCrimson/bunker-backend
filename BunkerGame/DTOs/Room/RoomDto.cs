namespace BunkerGame.DTOs.Room;

public class RoomDto
{
    public Guid Id { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public Guid HostId { get; set; }
    public int PlayerCount { get; set; }
    public DateTime CreatedAt { get; set; }
}