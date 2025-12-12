using System.ComponentModel.DataAnnotations;

namespace BunkerGame.DTOs.Profile;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Введите текущий пароль")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите новый пароль")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть не менее 6 символов")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите новый пароль")]
    [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;
}