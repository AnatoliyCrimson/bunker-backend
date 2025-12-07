using System.ComponentModel.DataAnnotations;

namespace BunkerGame.DTOs.Game;

public class RevealCardDto
{
    [Required]
    public Guid GameId { get; set; }
    
    [Required]
    public string TraitName { get; set; } = string.Empty; // Имя свойства, напр. "Profession"
}