using System.ComponentModel.DataAnnotations;

namespace BunkerGame.DTOs.Game;

public class VoteDto
{
    [Required]
    public Guid GameId { get; set; }
    
    [Required]
    public List<Guid> TargetPlayerIds { get; set; } = new();
}