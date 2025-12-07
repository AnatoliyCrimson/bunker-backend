using BunkerGame.Hubs;
using Microsoft.AspNetCore.SignalR;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps;

public class AnnounceVotingStep : IStepBody
{
    private readonly IHubContext<GameHub> _hubContext;

    public AnnounceVotingStep(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = context.PersistenceData as GameData;
        var currentStage = data.Stages[data.CurrentStageIndex];
        
        // Очищаем голоса для нового раунда
        data.CurrentRoundVotes.Clear();

        // Уведомляем фронтенд: пора голосовать!
        // Нужно выбрать (N-1) игроков.
        int votesNeeded = Math.Max(1, data.BunkerSpots - 1); // Минимум 1 голос

        await _hubContext.Clients.Group(data.GameId.ToString()).SendAsync("VotingStarted", new 
        { 
            voteWeight = currentStage.VoteWeight,
            votesToCast = votesNeeded 
        });

        return ExecutionResult.Next();
    }
}