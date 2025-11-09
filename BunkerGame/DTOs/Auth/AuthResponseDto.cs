using System.Text.Json.Serialization; // Для атрибутов сериализации, если нужно скрыть токен из JSON-ответа (не рекомендуется для Refresh токена, т.к. он идет в cookie)

namespace BunkerGame.DTOs.Auth;

/// <summary>
/// DTO для ответа на успешную аутентификацию (регистрация, вход, обновление токена).
/// </summary>
public class AuthResponseDto
{
    /// <summary>
    /// Сообщение о результате операции.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Пользовательские данные (опционально).
    /// </summary>
    public UserInfoDto? UserInfo { get; set; }

    /// <summary>
    /// Access токен (JWT), который клиент должен хранить в localStorage или памяти.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Время истечения Access токена (в UTC).
    /// </summary>
    public DateTime AccessTokenExpiry { get; set; }
}

/// <summary>
/// DTO для информации о пользователе, включаемой в AuthResponseDto.
/// </summary>
public class UserInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    // Добавьте другие поля, которые хотите вернуть клиенту
}