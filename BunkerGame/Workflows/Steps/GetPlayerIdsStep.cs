using BunkerGame.Data;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps;

public class GetPlayerIdsStep : StepBodyAsync
{
    private readonly IServiceScopeFactory _scopeFactory;

    public GetPlayerIdsStep(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Guid GameId { get; set; }
    public List<Guid> PlayerIds { get; set; } = new();

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var game = await dbContext.Games
                .Include(g => g.Players)
                .AsNoTracking() // Для чтения можно использовать AsNoTracking для скорости
                .FirstOrDefaultAsync(g => g.Id == GameId);

            if (game != null)
            {
                // Сортируем для стабильности порядка хода
                PlayerIds = game.Players
                    .OrderBy(p => p.Id) 
                    .Select(p => p.Id)
                    .ToList();
            }
        }

        return ExecutionResult.Next();
    }
}