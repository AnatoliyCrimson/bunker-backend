using System.ComponentModel.DataAnnotations;

namespace BunkerGame.DTOs.Room;

public class JoinRoomDto
{
    [Required]
    public Guid RoomId { get; set; }
    
    // Добавляем ID пользователя, которого нужно добавить
    [Required]
    public Guid UserId { get; set; }
}