using BunkerGame.Workflows.Steps;
using WorkflowCore.Interface;
using WorkflowCore.Models; // Нужно для ExecutionResult

namespace BunkerGame.Workflows;

public class GameWorkflow : IWorkflow<GameWorkflowData>
{
    public string Id => "BunkerGameWorkflow";
    public int Version => 1;

    public void Build(IWorkflowBuilder<GameWorkflowData> builder)
    {
        builder
            .StartWith<InitializeGameStep>()
                .Input(step => step.GameId, data => data.GameId)
            
            // Загружаем настройку. (Здесь ошибок не было, но на всякий случай оставим пустышку)
            .Then(
                // Небольшой хак, чтобы получить доступ к сервисам через DI внутри лямбды, 
                // но лучше сделать отдельный Step. Для краткости пока оставим так, 
                // но правильнее было бы сделать LoadGameConfigStep.
                // В WorkflowCore доступ к SP сложнее, поэтому предположим, 
                // что AdditionalRounds мы передали при старте workflow (см. GameService).
                // Если нет - по умолчанию 0.
                context => ExecutionResult.Next())

            // --- ВНЕШНИЙ ЦИКЛ (ЭТАПЫ) ---
            .While(data => data.StageLevel <= (3 + data.AdditionalRoundsConfig))
            .Do(stageLoop => stageLoop
                // 1. Вычисляем количество раундов
                .StartWith(context => 
                {
                    // !!! ВАЖНО: Явное приведение типа !!!
                    var data = (GameWorkflowData)context.Workflow.Data;
                    
                    if (data.StageLevel <= 3)
                    {
                        data.RoundsLeftInCurrentStage = 4 - data.StageLevel;
                    }
                    else
                    {
                        data.RoundsLeftInCurrentStage = 1;
                    }
                    return ExecutionResult.Next();
                })

                // --- ВНУТРЕННИЙ ЦИКЛ (РАУНДЫ) ---
                .While(data => data.RoundsLeftInCurrentStage > 0)
                .Do(roundLoop => roundLoop
                    // Фаза обсуждения
                    .StartWith<SetGamePhaseStep>()
                        .Input(step => step.GameId, data => data.GameId)
                        .Input(step => step.Phase, _ => "Opening")
                    
                    // Получаем список игроков
                    .Then<GetPlayerIdsStep>()
                        .Input(step => step.GameId, data => data.GameId)
                        .Output(data => data.PlayerIds, step => step.PlayerIds) 

                    // Цикл по игрокам
                    .ForEach(data => data.PlayerIds)
                    .Do(playerTurn => playerTurn
                        .StartWith<SetCurrentTurnStep>()
                            .Input(step => step.GameId, data => data.GameId)
                            .Input(step => step.PlayerId, (data, context) => (Guid)context.Item)
                        
                        // Ждем хода
                        .WaitFor("TraitRevealed", data => data.GameId.ToString())
                    )
                    
                    // Уменьшаем счетчик раундов
                    .Then(context => 
                    {
                        var data = (GameWorkflowData)context.Workflow.Data;
                        data.RoundsLeftInCurrentStage--;
                        return ExecutionResult.Next();
                    })
                )

                // --- ГОЛОСОВАНИЕ ---
                .Then<SetGamePhaseStep>()
                    .Input(step => step.GameId, data => data.GameId)
                    .Input(step => step.Phase, _ => "Voting")
                
                .WaitFor("VotingFinished", data => data.GameId.ToString())
                
                .Then<CalculateVotesStep>()
                    .Input(step => step.GameId, data => data.GameId)
                    .Input(step => step.Votes, data => data.CurrentVotes)
                    .Input(step => step.PointsPerVote, data => data.StageLevel <= 3 ? data.StageLevel : 3)
                
                // Переход к след. этапу
                .Then(context => 
                {
                    var data = (GameWorkflowData)context.Workflow.Data;
                    data.CurrentVotes.Clear();
                    data.StageLevel++;
                    return ExecutionResult.Next();
                })
            )
            // --- КОНЕЦ ---
            .Then<FinalizeGameStep>()
                .Input(step => step.GameId, data => data.GameId);
    }
}