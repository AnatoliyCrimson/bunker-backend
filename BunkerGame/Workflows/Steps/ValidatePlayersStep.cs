using WorkflowCore.Interface;
using WorkflowCore.Models;
using BunkerGame.Models;
// Workflows/Steps/ValidatePlayersStep.cs
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps
{
    public class ValidatePlayersStep : StepBodyAsync
    {
        public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            var data = context.Workflow.Data as GameData;
            var ctx = data.Context;

            // if (ctx.Players == null || ctx.Players.Count < 3)
            // {
                ctx.IsGameOver = true;
                throw new InvalidOperationException("Недостаточно игроков для начала игры (минимум 3).");
            // }

            ctx.CurrentPhase = "Players Validated";
            return ExecutionResult.Next();
        }
    }
}