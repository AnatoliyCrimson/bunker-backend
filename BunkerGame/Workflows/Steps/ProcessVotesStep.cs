using BunkerGame.Data;
using BunkerGame.Hubs;
using BunkerGame.Workflows;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps;

public class ProcessVotesStep : IStepBody
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<GameHub> _hubContext;

    public ProcessVotesStep(IServiceProvider serviceProvider, IHubContext<GameHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext stepContext)
    {
        var data = stepContext.PersistenceData as GameData;
        if (data == null) return ExecutionResult.Next();

        // Защита от null
        if (data.Stages == null || data.Stages.Count == 0) return ExecutionResult.Next();

        var currentStage = data.Stages[data.CurrentStageIndex];
        int weight = currentStage.VoteWeight;

        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var game = await dbContext.Games.Include(g => g.Players).FirstOrDefaultAsync(g => g.Id == data.GameId);
            
            if (game != null)
            {
                // Считаем голоса
                var roundScores = new Dictionary<Guid, int>(); 
                foreach (var voteEntry in data.CurrentRoundVotes)
                {
                    foreach (var targetId in voteEntry.Value)
                    {
                        if (!roundScores.ContainsKey(targetId)) roundScores[targetId] = 0;
                        roundScores[targetId] += weight; 
                    }
                }

                // Обновляем игроков
                foreach (var player in game.Players)
                {
                    if (roundScores.TryGetValue(player.UserId, out int score))
                    {
                        player.VoteCount += score; 
                    }
                }

                // ЛОГИКА ПЕРЕХОДА
                data.CurrentRoundVotes.Clear();
                data.RoundsPlayedInCurrentStage++;
            
                if (data.RoundsPlayedInCurrentStage >= currentStage.RoundsCount)
                {
                    data.CurrentStageIndex++;
                    data.RoundsPlayedInCurrentStage = 0; 
                }

                if (data.CurrentStageIndex >= data.Stages.Count)
                {
                    data.IsGameOver = true;
                    // Статус обновится в EndGameStep
                }
                else
                {
                    game.CurrentStep = data.Stages[data.CurrentStageIndex].Name; // Следующий этап
                }

                await dbContext.SaveChangesAsync();

                // Уведомляем клиентов
                await _hubContext.Clients.Group(data.GameId.ToString()).SendAsync("RoundResults", new
                {
                    scores = roundScores, 
                    totalScores = game.Players.Select(p => new { p.UserId, p.VoteCount }) 
                });
            }
        }

        return ExecutionResult.Next();
    }
}