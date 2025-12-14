using BunkerGame.Data;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps;

public class InitializeGameStep : StepBodyAsync
{
    private readonly IServiceScopeFactory _scopeFactory;

    // Внедряем фабрику вместо контекста
    public InitializeGameStep(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Guid GameId { get; set; }

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        // Создаем отдельный Scope для работы с базой в этом потоке
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try 
            {
                var game = await dbContext.Games
                    .Include(g => g.Players)
                    .FirstOrDefaultAsync(g => g.Id == GameId);

                if (game == null) 
                {
                    Console.WriteLine($"[InitializeGameStep] Game {GameId} not found!");
                    return ExecutionResult.Next();
                }

                // Логика шага
                game.AvailablePlaces = game.Players.Count / 2;
                if (game.AvailablePlaces == 0) game.AvailablePlaces = 1; // Защита, если игроков мало

                game.Phase = "Opening"; // Сразу переходим к обсуждению, так как Init завершен
                game.CurrentRoundNumber = 1;
                game.CurrentTurnPlayerId = null;

                await dbContext.SaveChangesAsync();
                Console.WriteLine($"[InitializeGameStep] Initialized game {GameId}. Places: {game.AvailablePlaces}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InitializeGameStep] ERROR: {ex.Message}");
                throw; // Пробрасываем ошибку, чтобы workflow увидел сбой
            }
        }

        return ExecutionResult.Next();
    }
}