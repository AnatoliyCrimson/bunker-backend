using BunkerGame.Data;
using BunkerGame.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps;

public class EndGameStep : IStepBody
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<GameHub> _hubContext;

    public EndGameStep(ApplicationDbContext context, IHubContext<GameHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = context.PersistenceData as GameData;
        var game = await _context.Games.Include(g => g.Players).ThenInclude(p => p.User).FirstOrDefaultAsync(g => g.Id == data.GameId);
        
        if (game == null) return ExecutionResult.Next();

        // 1. Сортируем игроков по очкам (по убыванию)
        // При равенстве очков используем Guid (или Random) для детерминированности,
        // но по правилам "случайный выбор".
        var sortedPlayers = game.Players.OrderByDescending(p => p.VoteCount).ToList();

        // 2. Определяем пограничный балл (балл игрока на N-м месте)
        // N = data.BunkerSpots
        int spots = data.BunkerSpots;
        
        // Список победителей
        var winners = new List<Guid>();
        
        if (spots >= sortedPlayers.Count)
        {
            // Все выжили (редкий случай)
            winners = sortedPlayers.Select(p => p.UserId).ToList();
        }
        else
        {
            // Берем топ N
            // Но нужно проверить ничью на грани
            // Пример: Мест 2. Баллы: 10, 8, 8, 5.
            // 2-е место имеет 8 баллов, 3-е тоже 8. Нужно кинуть жребий между ними.
            
            var boundaryScore = sortedPlayers[spots - 1].VoteCount;
            
            // Те, кто набрал строго больше пограничного - проходят 100%
            var guaranteedWinners = sortedPlayers.Where(p => p.VoteCount > boundaryScore).ToList();
            winners.AddRange(guaranteedWinners.Select(p => p.UserId));
            
            // Те, кто набрал пограничный балл (претенденты на оставшиеся места)
            var tieBreakers = sortedPlayers.Where(p => p.VoteCount == boundaryScore).ToList();
            
            int spotsLeft = spots - winners.Count;
            
            if (spotsLeft > 0)
            {
                // Случайным образом выбираем из претендентов
                var luckyWinners = tieBreakers.OrderBy(x => Guid.NewGuid()).Take(spotsLeft).ToList();
                winners.AddRange(luckyWinners.Select(p => p.UserId));
            }
        }

        // 3. Обновляем статус в БД
        foreach (var player in game.Players)
        {
            // IsKicked = true, если НЕ попал в список победителей
            player.IsKicked = !winners.Contains(player.UserId);
            
            // В конце открываем ВСЕ карты (Fog of War off)
            player.RevealedTraitKeys = new List<string> { "ALL" }; // Спец. флаг
        }
        await _context.SaveChangesAsync();

        // 4. Отправка итогов
        await _hubContext.Clients.Group(data.GameId.ToString()).SendAsync("GameEnded", new 
        { 
            winners = winners,
            finalScores = sortedPlayers.Select(p => new { p.UserId, p.VoteCount })
        });

        return ExecutionResult.Next();
    }
}