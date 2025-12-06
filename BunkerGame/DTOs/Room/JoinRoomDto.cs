using System.ComponentModel.DataAnnotations;

namespace BunkerGame.DTOs.Room;

public class JoinRoomDto
{
    [Required]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 characters long")]
    public string InviteCode { get; set; } = string.Empty;
}