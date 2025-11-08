using WorkflowCore.Interface;
using WorkflowCore.Models;
using BunkerGame.Models;
// Workflows/Steps/EliminatePlayerStep.cs
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps
{
    public class EliminatePlayerStep : StepBodyAsync
    {
        public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            var data = context.Workflow.Data as GameData;
            var ctx = data.Context;

            if (ctx.IsGameOver) return ExecutionResult.Next();

            ctx.CurrentPhase = "Elimination";

            // Логика исключения игрока
            // var eliminatedId = GetPlayerWithMostVotes(ctx.Votes);
            // ctx.EliminatedPlayerIds.Add(eliminatedId);

            return ExecutionResult.Next();
        }
    }
}