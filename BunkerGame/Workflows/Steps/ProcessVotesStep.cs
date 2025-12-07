using BunkerGame.Data;
using BunkerGame.Hubs;
using BunkerGame.Workflows;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Microsoft.Extensions.DependencyInjection;

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
        var currentStage = data.Stages[data.CurrentStageIndex];
        int weight = currentStage.VoteWeight;

        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var game = await dbContext.Games.Include(g => g.Players).FirstOrDefaultAsync(g => g.Id == data.GameId);
            
            if (game == null) return ExecutionResult.Next();

            // Подсчет голосов
            var roundScores = new Dictionary<Guid, int>(); 
            foreach (var voteEntry in data.CurrentRoundVotes)
            {
                foreach (var targetId in voteEntry.Value)
                {
                    if (!roundScores.ContainsKey(targetId)) roundScores[targetId] = 0;
                    roundScores[targetId] += weight; 
                }
            }

            // Обновление баллов
            foreach (var player in game.Players)
            {
                if (roundScores.TryGetValue(player.UserId, out int score))
                {
                    player.VoteCount += score; 
                }
            }

            // Логика переключения этапов (вычисляем новые индексы ПЕРЕД сохранением статуса)
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
            }

            // --- ОБНОВЛЕНИЕ СТАТУСА ---
            if (!data.IsGameOver)
            {
                var nextStageName = data.Stages[data.CurrentStageIndex].Name;
                game.CurrentStep = nextStageName; // Возвращаем "Stage X" (или следующий этап)
            }
            // --------------------------

            await dbContext.SaveChangesAsync();

            await _hubContext.Clients.Group(data.GameId.ToString()).SendAsync("RoundResults", new
            {
                scores = roundScores, 
                totalScores = game.Players.Select(p => new { p.UserId, p.VoteCount }) 
            });
        }

        return ExecutionResult.Next();
    }
}