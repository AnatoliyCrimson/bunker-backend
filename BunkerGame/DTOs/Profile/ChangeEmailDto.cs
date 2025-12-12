using System.ComponentModel.DataAnnotations;

namespace BunkerGame.DTOs.Profile;

public class ChangeEmailDto
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат Email")]
    public string NewEmail { get; set; } = string.Empty;
}