using BunkerGame.Data;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps;

public class SetGamePhaseStep : StepBodyAsync
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SetGamePhaseStep(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Guid GameId { get; set; }
    public string Phase { get; set; } = string.Empty;

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var game = await dbContext.Games.FindAsync(GameId);
            if (game != null)
            {
                game.Phase = Phase;
                if (Phase != "Discussion")
                {
                    game.CurrentTurnPlayerId = null;
                }
                await dbContext.SaveChangesAsync();
            }
        }
        return ExecutionResult.Next();
    }
}