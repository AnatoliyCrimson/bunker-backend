namespace BunkerGame.DTOs.Room;

public class RoomDetailsDto
{
    public Guid Id { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public Guid HostId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Список игроков с именами
    public List<RoomPlayerDto> Users { get; set; } = new();
    
    public Guid? GameId { get; set; }
    
    public bool IsGameStart { get; set; }
}

public class RoomPlayerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public string AvatarUrl { get; set; } = string.Empty;
}