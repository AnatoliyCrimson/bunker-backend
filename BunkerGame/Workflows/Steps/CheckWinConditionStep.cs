using WorkflowCore.Interface;
using WorkflowCore.Models;
using BunkerGame.Models;
// Workflows/Steps/CheckWinConditionStep.cs
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps
{
    public class CheckWinConditionStep : StepBodyAsync
    {
        public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            var data = context.Workflow.Data as GameData;
            var ctx = data.Context;

            if (ctx.IsGameOver) return ExecutionResult.Next();

            ctx.CurrentPhase = "Checking Win Condition";

            // Логика проверки победы
            // ctx.IsGameOver = IsWinConditionMet(ctx);

            return ExecutionResult.Next();
        }
    }
}