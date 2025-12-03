using WorkflowCore.Interface;

namespace BunkerGame.Workflows;

// Убедись, что используешь именно этот GameData
public class GameWorkflow : IWorkflow<GameData>
{
    public string Id => "BunkerGameWorkflow";
    public int Version => 1;

    public void Build(IWorkflowBuilder<GameData> builder)
    {
        // Временная заглушка. Workflow просто запускается и останавливается.
        // Позже мы добавим сюда реальные шаги (Steps).
        builder
            .StartWith(context => Console.WriteLine("Game Workflow Started"))
            .Then(context => Console.WriteLine("Game Workflow Finished"));
    }
}