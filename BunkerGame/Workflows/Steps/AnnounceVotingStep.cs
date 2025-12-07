using BunkerGame.Data; // Добавлено
using BunkerGame.Hubs;
using BunkerGame.Workflows;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore; // Добавлено
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Microsoft.Extensions.DependencyInjection; // Добавлено

namespace BunkerGame.Workflows.Steps;

public class AnnounceVotingStep : IStepBody
{
    private readonly IServiceProvider _serviceProvider; // Изменено
    private readonly IHubContext<GameHub> _hubContext;

    public AnnounceVotingStep(IServiceProvider serviceProvider, IHubContext<GameHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext stepContext)
    {
        var data = stepContext.PersistenceData as GameData;
        var currentStage = data.Stages[data.CurrentStageIndex];
        
        data.CurrentRoundVotes.Clear();

        // --- ОБНОВЛЕНИЕ СТАТУСА В БД ---
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var game = await dbContext.Games.FindAsync(data.GameId);
            if (game != null)
            {
                game.CurrentStep = $"{currentStage.Name} - Voting"; // Пример: "Stage 1 - Voting"
                await dbContext.SaveChangesAsync();
            }
        }
        // -------------------------------

        int votesNeeded = data.VotesRequiredPerPlayer;

        await _hubContext.Clients.Group(data.GameId.ToString()).SendAsync("VotingStarted", new 
        { 
            voteWeight = currentStage.VoteWeight,
            votesToCast = votesNeeded 
        });

        return ExecutionResult.Next();
    }
}