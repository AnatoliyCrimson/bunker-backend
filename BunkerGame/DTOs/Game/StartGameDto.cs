using System.ComponentModel.DataAnnotations;

namespace BunkerGame.DTOs.Game;

public class StartGameDto
{
    [Required]
    public Guid RoomId { get; set; }
    public int AdditionalRounds { get; set; } = 0;
}