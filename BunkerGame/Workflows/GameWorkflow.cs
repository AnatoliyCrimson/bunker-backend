// Workflows/GameWorkflow.cs
using WorkflowCore.Interface;
using WorkflowCore.Models;
using BunkerGame.Models;
// Workflows/GameWorkflow.cs
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows
{
    public class GameWorkflow : IWorkflow<GameData>
    {
        public void Build(IWorkflowBuilder<GameData> builder)
        {
            builder
                .StartWith<Steps.ValidatePlayersStep>()
                .Then<Steps.InitializeGameStep>()
                .Then<Steps.RevealRolesStep>()
                .While(data => !data.Context.IsGameOver)
                .Do(x => x
                    .StartWith<Steps.StartRoundStep>()
                    .Then<Steps.PlayerIntroductionStep>()
                    .Then<Steps.DiscussionStep>()
                    .Then<Steps.VotingStep>()
                    .Then<Steps.CountVotesStep>()
                    .Then<Steps.EliminatePlayerStep>()
                    .Then<Steps.BunkerSelectionStep>()
                    .Then<Steps.CheckWinConditionStep>()
                    .Then<Steps.UpdateGameStateStep>()
                    .Then<Steps.NextRoundOrEndStep>()
                )
                .Then<Steps.EndGameStep>();
        }

        public string Id => "BunkerGameWorkflow";
        public int Version => 1;
    }
}