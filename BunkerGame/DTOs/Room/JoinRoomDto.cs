using System.ComponentModel.DataAnnotations;

namespace BunkerGame.DTOs.Room;

public class JoinRoomDto
{
    [Required]
    public Guid RoomId { get; set; }
}