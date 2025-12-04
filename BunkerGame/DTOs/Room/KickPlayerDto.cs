using System.ComponentModel.DataAnnotations;

namespace BunkerGame.DTOs.Room;

public class KickPlayerDto
{
    [Required]
    public Guid RoomId { get; set; }
    
    [Required]
    public Guid UserId { get; set; } // Кого удаляем
}