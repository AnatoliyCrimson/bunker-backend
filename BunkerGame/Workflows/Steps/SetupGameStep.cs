using BunkerGame.Data;
using BunkerGame.Hubs;
using BunkerGame.Workflows; 
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Microsoft.Extensions.DependencyInjection; // <-- Важный using

namespace BunkerGame.Workflows.Steps;

public class SetupGameStep : IStepBody
{
    private readonly IServiceProvider _serviceProvider; // <-- Вместо DbContext
    private readonly IHubContext<GameHub> _hubContext;

    public SetupGameStep(IServiceProvider serviceProvider, IHubContext<GameHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext stepContext)
    {
        var data = stepContext.PersistenceData as GameData;
        
        // --- СОЗДАЕМ РУЧНОЙ SCOPE ДЛЯ РАБОТЫ С БД ---
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Загружаем игру
            var game = await dbContext.Games
                .Include(g => g.Players)
                .FirstOrDefaultAsync(g => g.Id == data.GameId);
            
            if (game == null) return ExecutionResult.Next();

            // 1. Считаем места в бункере (N)
            data.PlayersCount = game.Players.Count;
            data.BunkerSpots = data.PlayersCount / 2;
            data.VotesRequiredPerPlayer = Math.Max(1, data.BunkerSpots - 1);

            // 2. Формируем порядок хода
            data.TurnOrder = game.Players
                .Select(p => p.UserId)
                .OrderBy(x => Guid.NewGuid()) 
                .ToList();

            // 3. Настраиваем этапы
            data.Stages = new List<StageConfig>
            {
                new StageConfig { Name = "Этап 1", RoundsCount = 3, VoteWeight = 1 },
                new StageConfig { Name = "Этап 2", RoundsCount = 2, VoteWeight = 2 },
                new StageConfig { Name = "Этап 3", RoundsCount = 1, VoteWeight = 3 }
            };
            
            data.CurrentStageIndex = 0;
            data.RoundsPlayedInCurrentStage = 0;

            // Уведомляем клиентов
            await _hubContext.Clients.Group(data.GameId.ToString()).SendAsync("GameStarted", new 
            { 
                bunkerSpots = data.BunkerSpots,
                votesRequired = data.VotesRequiredPerPlayer,
                turnOrder = data.TurnOrder,
                stagesCount = data.Stages.Count
            });
        }
        // Scope уничтожается здесь, подключение к БД закрывается корректно

        return ExecutionResult.Next();
    }
}