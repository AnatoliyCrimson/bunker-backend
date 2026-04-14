using System.ComponentModel.DataAnnotations;

namespace BunkerGame.DTOs.Game;

// Универсальная DTO для действий (открыть карту, проголосовать)
public class PresentationCardDto
{
    [Required]
    public string TraitCode { get; set; } = string.Empty; // Имя свойства, напр. "Profession"
}

public class VoteDto
{
    
    [Required]
    public List<Guid> TargetsPlayerId { get; set; } // За кого голосуем
}