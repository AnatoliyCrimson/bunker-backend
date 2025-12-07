using BunkerGame.Data;
using BunkerGame.Hubs;
using BunkerGame.Workflows; // Используем правильный namespace
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Microsoft.Extensions.DependencyInjection;

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
        // 1. Явное приведение типов с полным путем
        var data = stepContext.PersistenceData as BunkerGame.Workflows.GameData;

        // 2. ЗАЩИТА ОТ NULL (Если данные потерялись)
        if (data == null)
        {
            Console.WriteLine("ERROR: GameData is null in AnnounceVotingStep. Attempting to recover...");
            return ExecutionResult.Next(); // Пропускаем шаг, чтобы не крашить игру
        }

        // 3. Инициализация списков, если они null
        if (data.Stages == null || data.Stages.Count == 0)
        {
            // Восстанавливаем дефолтные этапы (fallback)
            data.Stages = new List<StageConfig>
            {
                new StageConfig { Name = "Stage 1", RoundsCount = 3, VoteWeight = 1 },
                new StageConfig { Name = "Stage 2", RoundsCount = 2, VoteWeight = 2 },
                new StageConfig { Name = "Stage 3", RoundsCount = 1, VoteWeight = 3 }
            };
        }

        if (data.CurrentRoundVotes == null)
        {
            data.CurrentRoundVotes = new Dictionary<Guid, List<Guid>>();
        }
        
        // 4. Безопасное получение текущего этапа
        if (data.CurrentStageIndex >= data.Stages.Count)
        {
             data.CurrentStageIndex = data.Stages.Count - 1; // Защита от выхода за границы
        }
        var currentStage = data.Stages[data.CurrentStageIndex];
        
        data.CurrentRoundVotes.Clear();

        // 5. Обновление БД
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

        int votesNeeded = data.VotesRequiredPerPlayer;

        await _hubContext.Clients.Group(data.GameId.ToString()).SendAsync("VotingStarted", new 
        { 
            voteWeight = currentStage.VoteWeight,
            votesToCast = votesNeeded 
        });

        return ExecutionResult.Next();
    }
}