using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BunkerGame.Hubs;

[Authorize]
public class GameHub : Hub
{
    // Клиент вызывает этот метод после подключения к SignalR, 
    // чтобы подписаться на события конкретной игры
    public async Task JoinGameGroup(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
    }
    
    // Метод для выхода из группы (опционально, SignalR сам чистит при дисконнекте)
    public async Task LeaveGameGroup(string gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
    }
}