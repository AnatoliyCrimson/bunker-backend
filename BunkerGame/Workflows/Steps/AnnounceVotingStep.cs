using BunkerGame.Data;
using BunkerGame.Hubs;
using BunkerGame.Workflows;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps;

public class AnnounceVotingStep : IStepBody
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<GameHub> _hubContext;

    public AnnounceVotingStep(IServiceProvider serviceProvider, IHubContext<GameHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext stepContext)
    {
        var data = stepContext.PersistenceData as GameData;
        if (data == null) return ExecutionResult.Next();

        // --- ВОССТАНОВЛЕНИЕ ДАННЫХ (Fallback) ---
        if (data.Stages == null || data.Stages.Count == 0)
        {
            data.Stages = new List<StageConfig>
            {
                new StageConfig { Name = "Stage 1", RoundsCount = 3, VoteWeight = 1 },
                new StageConfig { Name = "Stage 2", RoundsCount = 2, VoteWeight = 2 },
                new StageConfig { Name = "Stage 3", RoundsCount = 1, VoteWeight = 3 }
            };
        }
        if (data.CurrentRoundVotes == null) data.CurrentRoundVotes = new Dictionary<Guid, List<Guid>>();
        // ----------------------------------------

        if (data.CurrentStageIndex >= data.Stages.Count) data.CurrentStageIndex = data.Stages.Count - 1;
        
        var currentStage = data.Stages[data.CurrentStageIndex];
        
        // Очищаем голоса перед началом голосования
        data.CurrentRoundVotes.Clear();

        // Обновляем статус в БД
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var game = await dbContext.Games.FindAsync(data.GameId);
            if (game != null)
            {
                game.CurrentStep = $"{currentStage.Name} - Voting";
                await dbContext.SaveChangesAsync();
            }
        }

        await _hubContext.Clients.Group(data.GameId.ToString()).SendAsync("VotingStarted", new 
        { 
            voteWeight = currentStage.VoteWeight,
            votesToCast = data.VotesRequiredPerPlayer 
        });

        return ExecutionResult.Next();
    }
}