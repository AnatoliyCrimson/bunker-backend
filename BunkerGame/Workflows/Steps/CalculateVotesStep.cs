using BunkerGame.Data;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps;

public class CalculateVotesStep : StepBodyAsync
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CalculateVotesStep(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Guid GameId { get; set; }
    public int PointsPerVote { get; set; }
    public Dictionary<Guid, Guid> Votes { get; set; } = new();

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        if (Votes == null || Votes.Count == 0) return ExecutionResult.Next();

        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var game = await dbContext.Games
                .Include(g => g.Players)
                .FirstOrDefaultAsync(g => g.Id == GameId);

            if (game != null)
            {
                foreach (var vote in Votes)
                {
                    var targetId = vote.Value;
                    var targetPlayer = game.Players.FirstOrDefault(p => p.Id == targetId);
                    
                    if (targetPlayer != null)
                    {
                        targetPlayer.TotalScore += PointsPerVote;
                    }
                }
                
                await dbContext.SaveChangesAsync();
            }
        }
        
        return ExecutionResult.Next();
    }
}