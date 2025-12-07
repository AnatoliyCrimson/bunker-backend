using BunkerGame.Data;
using BunkerGame.Hubs;
using BunkerGame.Workflows; // Для GameData и StageConfig
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps;

public class SetupGameStep : IStepBody
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<GameHub> _hubContext;

    public SetupGameStep(ApplicationDbContext context, IHubContext<GameHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = context.PersistenceData as GameData;
        
        // Загружаем игру, чтобы узнать кол-во игроков
        var game = await _context.Games
            .Include(g => g.Players)
            .FirstOrDefaultAsync(g => g.Id == data.GameId);
        
        if (game == null) return ExecutionResult.Next();

        // 1. Считаем места в бункере (N)
        data.PlayersCount = game.Players.Count;
        
        // "половина от количества игроков (если их нечётное количество, то округление в меньшую сторону)"
        // Math.Floor не нужен для интов (деление интов и так отбрасывает дробную часть), но для наглядности:
        data.BunkerSpots = data.PlayersCount / 2;

        // 2. Сколько голосов должен отдать каждый (N-1)
        // Минимум 1 голос (если мест 1, то 0 голосов быть не может по логике игры, но пусть будет 1)
        data.VotesRequiredPerPlayer = Math.Max(1, data.BunkerSpots - 1);

        // 3. Формируем порядок хода (перемешиваем)
        data.TurnOrder = game.Players
            .Select(p => p.UserId)
            .OrderBy(x => Guid.NewGuid()) // Random shuffle
            .ToList();

        // 4. Настраиваем этапы по правилам
        data.Stages = new List<StageConfig>
        {
            // Этап 1: 3 раунда (открытий), вес голоса 1
            new StageConfig { Name = "Этап 1", RoundsCount = 3, VoteWeight = 1 },
            
            // Этап 2: 2 раунда (открытий), вес голоса 2
            new StageConfig { Name = "Этап 2", RoundsCount = 2, VoteWeight = 2 },
            
            // Этап 3: 1 раунд (открытий), вес голоса 3
            new StageConfig { Name = "Этап 3", RoundsCount = 1, VoteWeight = 3 }
            
            // Сюда можно программно добавить Доп. Раунды, если игра это поддерживает в настройках DTO
        };
        
        data.CurrentStageIndex = 0;
        data.RoundsPlayedInCurrentStage = 0;

        // Уведомляем клиентов через SignalR
        await _hubContext.Clients.Group(data.GameId.ToString()).SendAsync("GameStarted", new 
        { 
            bunkerSpots = data.BunkerSpots,
            votesRequired = data.VotesRequiredPerPlayer,
            turnOrder = data.TurnOrder,
            stagesCount = data.Stages.Count
        });

        return ExecutionResult.Next();
    }
}