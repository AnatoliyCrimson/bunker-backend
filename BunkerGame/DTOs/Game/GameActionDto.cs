using System.ComponentModel.DataAnnotations;

namespace BunkerGame.DTOs.Game;

// Универсальная DTO для действий (открыть карту, проголосовать)
public class RevealCardDto
{
    [Required]
    public Guid GameId { get; set; }
    
    [Required]
    public string TraitName { get; set; } = string.Empty; // Имя свойства, напр. "Profession"
}

public class VoteDto
{
    [Required]
    public Guid GameId { get; set; }
    
    [Required]
    public Guid TargetPlayerId { get; set; } // За кого голосуем
}