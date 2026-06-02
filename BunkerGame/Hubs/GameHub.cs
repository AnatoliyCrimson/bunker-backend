using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace BunkerGame.Hubs;

[Authorize]
public class GameHub : Hub
{
    // Метод для присоединения к группе (комнате)
    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        // Можно добавить уведомление, если нужно (но пока по плану только инфраструктура)
    }

    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
    }
}