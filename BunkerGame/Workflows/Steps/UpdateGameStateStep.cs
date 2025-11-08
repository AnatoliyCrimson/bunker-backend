using WorkflowCore.Interface;
using WorkflowCore.Models;
using BunkerGame.Models;
// Workflows/Steps/UpdateGameStateStep.cs
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps
{
    public class UpdateGameStateStep : StepBodyAsync
    {
        public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            var data = context.Workflow.Data as GameData;
            var ctx = data.Context;

            if (ctx.IsGameOver) return ExecutionResult.Next();

            ctx.CurrentPhase = "Updating State";

            return ExecutionResult.Next();
        }
    }
}