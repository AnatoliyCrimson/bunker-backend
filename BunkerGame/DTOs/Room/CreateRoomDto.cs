using System.ComponentModel.DataAnnotations;

namespace BunkerGame.DTOs.Room;

public class CreateRoomDto
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
}