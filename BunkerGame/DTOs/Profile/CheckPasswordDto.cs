using System.ComponentModel.DataAnnotations;

namespace BunkerGame.DTOs.Profile;

public class CheckPasswordDto
{
    [Required(ErrorMessage = "Введите пароль")]
    public string Password { get; set; } = string.Empty;
}