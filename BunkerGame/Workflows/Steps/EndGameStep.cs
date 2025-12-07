using BunkerGame.Data;
using BunkerGame.Hubs;
using BunkerGame.Workflows;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BunkerGame.Workflows.Steps;

public class EndGameStep : IStepBody
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<GameHub> _hubContext;

    public EndGameStep(IServiceProvider serviceProvider, IHubContext<GameHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext stepContext)
    {
        var data = stepContext.PersistenceData as GameData;
        
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var game = await dbContext.Games
                .Include(g => g.Players)
                .FirstOrDefaultAsync(g => g.Id == data.GameId);
            
            if (game == null) return ExecutionResult.Next();

            // ... (Логика победителей та же самая) ...
            var sortedPlayers = game.Players.OrderByDescending(p => p.VoteCount).ToList();
            int spots = data.BunkerSpots;
            var winners = new List<Guid>();

            if (spots >= sortedPlayers.Count) {
                winners = sortedPlayers.Select(p => p.UserId).ToList();
            } else {
                var boundaryScore = sortedPlayers[spots - 1].VoteCount;
                var guaranteedWinners = sortedPlayers.Where(p => p.VoteCount > boundaryScore).ToList();
                winners.AddRange(guaranteedWinners.Select(p => p.UserId));
                
                var tieBreakers = sortedPlayers.Where(p => p.VoteCount == boundaryScore).ToList();
                int spotsLeft = spots - winners.Count;
                if (spotsLeft > 0) {
                    var luckyWinners = tieBreakers.OrderBy(x => Guid.NewGuid()).Take(spotsLeft).ToList();
                    winners.AddRange(luckyWinners.Select(p => p.UserId));
                }
            }

            foreach (var player in game.Players)
            {
                player.IsKicked = !winners.Contains(player.UserId);
                player.RevealedTraitKeys = new List<string> { "ALL" }; 
            }

            // --- ОБНОВЛЕНИЕ СТАТУСА ---
            game.CurrentStep = "Game Over";
            // --------------------------

            await dbContext.SaveChangesAsync();

            await _hubContext.Clients.Group(data.GameId.ToString()).SendAsync("GameEnded", new 
            { 
                winners = winners,
                finalScores = sortedPlayers.Select(p => new { p.UserId, p.VoteCount })
            });
        }

        return ExecutionResult.Next();
    }
}