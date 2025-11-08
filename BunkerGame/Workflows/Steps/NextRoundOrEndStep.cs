using WorkflowCore.Interface;
using WorkflowCore.Models;
using BunkerGame.Models;
// Workflows/Steps/NextRoundOrEndStep.cs
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps
{
    public class NextRoundOrEndStep : StepBodyAsync
    {
        public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            var data = context.Workflow.Data as GameData;
            var ctx = data.Context;

            if (ctx.IsGameOver) return ExecutionResult.Next();

            if (ctx.CurrentRound < ctx.MaxRounds)
            {
                ctx.CurrentRound++;
                ctx.CurrentPhase = "Next Round";
                return ExecutionResult.Next();
            }

            ctx.IsGameOver = true;
            ctx.CurrentPhase = "Max Rounds Reached";
            return ExecutionResult.Next();
        }
    }
}