using System.ComponentModel.DataAnnotations;
namespace BunkerGame.DTOs.Room;

public class LeaveRoomDto
{
    [Required]
    public Guid RoomId { get; set; }
}