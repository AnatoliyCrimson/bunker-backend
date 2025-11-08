using WorkflowCore.Interface;
using WorkflowCore.Models;
using BunkerGame.Models;
// Workflows/Steps/InitializeGameStep.cs
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps
{
    public class InitializeGameStep : StepBodyAsync
    {
        public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            var data = context.Workflow.Data as GameData;
            var ctx = data.Context;

            if (ctx.IsGameOver) return ExecutionResult.Next();

            ctx.CurrentRound = 1;
            ctx.EliminatedPlayerIds.Clear();
            ctx.BunkerPlayers.Clear();
            ctx.Votes.Clear();
            ctx.CurrentPhase = "Initialization";

            return ExecutionResult.Next();
        }
    }
}