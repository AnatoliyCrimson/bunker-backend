using BunkerGame.Hubs;
using BunkerGame.Workflows;
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
        if (data == null) return ExecutionResult.Next();

        // Защита от выхода за границы массива
        if (data.TurnOrder == null || data.TurnOrder.Count == 0) return ExecutionResult.Next();
        if (data.CurrentPlayerTurnIndex >= data.TurnOrder.Count) data.CurrentPlayerTurnIndex = 0;

        var currentPlayerId = data.TurnOrder[data.CurrentPlayerTurnIndex];

        // Защита от null в Stages
        string stageName = "Unknown";
        if (data.Stages != null && data.CurrentStageIndex < data.Stages.Count)
        {
            stageName = data.Stages[data.CurrentStageIndex].Name;
        }

        await _hubContext.Clients.Group(data.GameId.ToString()).SendAsync("TurnStarted", new 
        { 
            userId = currentPlayerId,
            stage = stageName,
            roundNumber = data.RoundsPlayedInCurrentStage + 1
        });

        return ExecutionResult.Next();
    }
}