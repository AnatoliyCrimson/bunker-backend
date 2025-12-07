using BunkerGame.Hubs;
using Microsoft.AspNetCore.SignalR;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps;

public class AnnounceTurnStep : IStepBody
{
    private readonly IHubContext<GameHub> _hubContext;

    public AnnounceTurnStep(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = context.PersistenceData as GameData;
        var currentPlayerId = data.TurnOrder[data.CurrentPlayerTurnIndex];

        // Отправляем событие "TurnStarted"
        await _hubContext.Clients.Group(data.GameId.ToString()).SendAsync("TurnStarted", new 
        { 
            userId = currentPlayerId,
            stage = data.Stages[data.CurrentStageIndex].Name,
            roundNumber = data.RoundsPlayedInCurrentStage + 1
        });

        return ExecutionResult.Next();
    }
}