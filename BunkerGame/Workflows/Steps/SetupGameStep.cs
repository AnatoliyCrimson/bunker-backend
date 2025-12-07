using BunkerGame.Data;
using BunkerGame.Hubs;
using BunkerGame.Workflows;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BunkerGame.Workflows.Steps;

public class SetupGameStep : IStepBody
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<GameHub> _hubContext;

    public SetupGameStep(IServiceProvider serviceProvider, IHubContext<GameHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext stepContext)
    {
        var data = stepContext.PersistenceData as GameData;
        
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var game = await dbContext.Games
                .Include(g => g.Players)
                .FirstOrDefaultAsync(g => g.Id == data.GameId);
            
            if (game == null) return ExecutionResult.Next();

            // Настройка логики
            data.PlayersCount = game.Players.Count;
            data.BunkerSpots = data.PlayersCount / 2;
            data.VotesRequiredPerPlayer = Math.Max(1, data.BunkerSpots - 1);
            
            data.TurnOrder = game.Players.Select(p => p.UserId).OrderBy(x => Guid.NewGuid()).ToList();

            data.Stages = new List<StageConfig>
            {
                new StageConfig { Name = "Stage 1", RoundsCount = 3, VoteWeight = 1 },
                new StageConfig { Name = "Stage 2", RoundsCount = 2, VoteWeight = 2 },
                new StageConfig { Name = "Stage 3", RoundsCount = 1, VoteWeight = 3 }
            };
            
            data.CurrentStageIndex = 0;
            data.RoundsPlayedInCurrentStage = 0;

            // --- ОБНОВЛЕНИЕ СТАТУСА В БД ---
            game.CurrentStep = data.Stages[0].Name; // "Stage 1"
            await dbContext.SaveChangesAsync();
            // -------------------------------

            await _hubContext.Clients.Group(data.GameId.ToString()).SendAsync("GameStarted", new 
            { 
                bunkerSpots = data.BunkerSpots,
                turnOrder = data.TurnOrder
            });
        }

        return ExecutionResult.Next();
    }
}