using BunkerGame.Data;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps;

public class FinalizeGameStep : StepBodyAsync
{
    private readonly IServiceScopeFactory _scopeFactory;

    public FinalizeGameStep(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Guid GameId { get; set; }

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var game = await dbContext.Games
                .Include(g => g.Players)
                .FirstOrDefaultAsync(g => g.Id == GameId);

            if (game == null) return ExecutionResult.Next();

            game.Phase = "GameOver";
            game.CurrentTurnPlayerId = null;

            // ЛОГИКА ОПРЕДЕЛЕНИЯ ПОБЕДИТЕЛЕЙ
            // 1. Сортируем по очкам (TotalScore) по убыванию.
            // 2. Берем топ N (AvailablePlaces).
            // 3. (Опционально) При равенстве очков нужен рандом, но пока OrderByDescending сделает детерминировано.
            
            var winners = game.Players
                .OrderByDescending(p => p.TotalScore)
                .Take(game.AvailablePlaces)
                .ToList();
            
            // Для наглядности можно открыть всем все характеристики в конце
            foreach (var player in game.Players)
            {
                // Здесь можно добавить логику открытия всех карт
                // player.RevealedTraitKeys.Add("ALL"); 
            }

            await dbContext.SaveChangesAsync();
        }
        return ExecutionResult.Next();
    }
}