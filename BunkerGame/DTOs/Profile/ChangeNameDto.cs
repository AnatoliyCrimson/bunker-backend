using System.ComponentModel.DataAnnotations;

namespace BunkerGame.DTOs.Profile;

public class ChangeNameDto
{
    [Required(ErrorMessage = "Имя обязательно")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Имя должно быть от 2 до 50 символов")]
    public string NewName { get; set; } = string.Empty;
}