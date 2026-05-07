using System.ComponentModel.DataAnnotations;

namespace BunkerGame.DTOs.Ai;

public class EditAiPromptDto
{
    [Required]
    public string Code { get; set; } = null!;

    [Required]
    public string SystemPrompt { get; set; } = null!;

    [Required]
    public string UserPromptTemplate { get; set; } = null!;

    public float Temperature { get; set; } = 0.6f;
}