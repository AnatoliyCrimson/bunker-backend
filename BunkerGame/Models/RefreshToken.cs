using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BunkerGame.Models;

public class RefreshToken
{
    [Key]
    public int Id { get; set; } // Уникальный ID токена в БД

    [Required]
    [MaxLength(256)] // Длина может варьироваться, но обычно токены довольно длинные
    public string Token { get; set; } = string.Empty; // Сам Refresh токен (случайная строка)

    [Required]
    public Guid UserId { get; set; } // Ссылка на пользователя

    [ForeignKey("UserId")]
    public User User { get; set; } = null!; // Навигационное свойство

    public DateTime ExpiresAt { get; set; } // Время истечения

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Время создания

    public DateTime? RevokedAt { get; set; } // Время отзыва (null если не отозван)

    public string? RevokedByIp { get; set; } // IP, с которого был отозван (опционально)

    public string? ReplacedByToken { get; set; } // Если токен был заменен на новый, хранится новый токен

    // Вспомогательное свойство для проверки статуса
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}