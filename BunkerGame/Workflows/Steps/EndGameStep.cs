using WorkflowCore.Interface;
using WorkflowCore.Models;
using BunkerGame.Models;
// Workflows/Steps/EndGameStep.cs
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps
{
    public class EndGameStep : StepBodyAsync
    {
        public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            var data = context.Workflow.Data as GameData;
            var ctx = data.Context;

            ctx.CurrentPhase = "Game Over";

            return ExecutionResult.Next();
        }
    }
}