namespace BunkerGame.Services;

public interface IGameService
{
    Task<Guid> StartGameAsync(Guid roomId, Guid userId, int additionalRounds);
}