using BunkerGame.DTOs.Game;
using BunkerGame.Models;

namespace BunkerGame.Services;

public class GameService : IGameService
{
    public Task<Guid> StartGameAsync(Guid roomId)
    {
        throw new NotImplementedException();
    }

    public Task<object> GetGameStateForUserAsync(Guid gameId, Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task RevealTraitAsync(Guid gameId, Guid userId, string traitName)
    {
        throw new NotImplementedException();
    }

    public Task VoteAsync(Guid gameId, Guid userId, Guid targetPlayerId)
    {
        throw new NotImplementedException();
    }
}