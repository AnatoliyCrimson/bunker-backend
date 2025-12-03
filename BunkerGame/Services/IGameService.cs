using BunkerGame.DTOs.Game;
using BunkerGame.Models;

namespace BunkerGame.Services;

public interface IGameService
{
    // Запуск
    Task<Guid> StartGameAsync(Guid roomId);
    
    // Получение состояния (с учетом Fog of War)
    Task<object> GetGameStateForUserAsync(Guid gameId, Guid userId);
    
    // Действия (для PlayController)
    Task RevealTraitAsync(Guid gameId, Guid userId, string traitName);
    Task VoteAsync(Guid gameId, Guid userId, Guid targetPlayerId);
}