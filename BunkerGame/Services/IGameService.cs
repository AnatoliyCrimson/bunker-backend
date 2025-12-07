using BunkerGame.DTOs.Game;
using BunkerGame.Models;

namespace BunkerGame.Services;

public interface IGameService
{
    // Запуск
    Task<Guid> StartGameAsync(Guid roomId);
    
    // Получение состояния
    Task<object> GetGameStateForUserAsync(Guid gameId, Guid userId);
    
    // Действия
    Task RevealTraitAsync(Guid gameId, Guid userId, string traitName);
    
    // ИСПРАВЛЕНО: Теперь принимает List<Guid>
    Task VoteAsync(Guid gameId, Guid userId, List<Guid> targetPlayerIds);
}