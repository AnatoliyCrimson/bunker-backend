namespace BunkerGame.DTOs.Game;

public class GameDto
{
    public Guid Id { get; set; }
    public string? CurrentStep { get; set; }
    public int PlayerCount { get; set; }
    public DateTime StartedAt { get; set; }
}