namespace BunkerGame.Models;

public class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string InviteCode { get; set; } = string.Empty;

    public Guid HostId { get; set; } // ID создателя (для прав управления)

    // --- ИЗМЕНЕНИЯ ---
    // Вместо List<Guid> PlayerIds используем навигационное свойство.
    // EF Core сам заполнит этот список пользователями, у которых CurrentRoomId == this.Id
    public ICollection<User> Players { get; set; } = new List<User>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}