namespace BunkerGame.Services.GameService;

public interface IGameService
{
    Task<Guid> StartGameAsync(Guid userId, int additionalRounds);
    Task PresentationTrait(Guid userId, string traitCode);
    Task EndDiscussion(Guid userId);
    Task SubmitVote(Guid userId, List<Guid> votedPlayerIds);
    Task EndStoryPhaseAsync(Guid userId);
    Task<object> GetGameStateAsync(Guid userId);
    Task<List<BunkerGame.DTOs.Game.GameDto>> GetAllGamesAsync();
    Task<bool> DeleteGameAsync(Guid gameId, Guid userId);
}