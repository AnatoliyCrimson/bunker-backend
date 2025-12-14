using BunkerGame.Data;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps;

public class SetCurrentTurnStep : StepBodyAsync
{
    private readonly ApplicationDbContext _context;

    public SetCurrentTurnStep(ApplicationDbContext context)
    {
        _context = context;
    }

    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; } // ID игрока, чей сейчас ход

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var game = await _context.Games.FindAsync(GameId);
        if (game == null) return ExecutionResult.Next();

        game.CurrentTurnPlayerId = PlayerId;
        
        await _context.SaveChangesAsync();
        return ExecutionResult.Next();
    }
}