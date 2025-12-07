using System;
using System.Collections.Generic;
using BunkerGame.Workflows.Steps;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using BunkerGame.Workflows.Events;

namespace BunkerGame.Workflows;

public class GameWorkflow : IWorkflow<GameData>
{
    public string Id => "BunkerGameWorkflow";
    public int Version => 1;

    public void Build(IWorkflowBuilder<GameData> builder)
    {
        builder
            .StartWith<SetupGameStep>()
            
            // --- ГЛАВНЫЙ ЦИКЛ ИГРЫ (Пока игра не закончена) ---
            .While(data => !data.IsGameOver)
            .Do(stageFlow => stageFlow
                
                // 1. Инициализация раунда: сбрасываем индекс игрока на 0
                .StartWith(context => {
                    // Явное указание пространства имен для защиты от путаницы
                    var data = context.PersistenceData as BunkerGame.Workflows.GameData;
                    
                    // ЗАЩИТА ОТ NULL
                    if (data != null)
                    {
                        data.CurrentPlayerTurnIndex = 0;
                    }
                    
                    return ExecutionResult.Next();
                })

                // 2. ЦИКЛ ХОДОВ (Пока индекс меньше кол-ва игроков)
                .While(data => data.CurrentPlayerTurnIndex < data.PlayersCount)
                .Do(turnFlow => turnFlow
                    // Объявляем чей ход
                    .StartWith<AnnounceTurnStep>()
                    
                    // Ждем события "RevealAction" от GameService
                    .WaitFor("RevealAction", data => data.GameId.ToString())
                    
                    // После события увеличиваем индекс игрока
                    .Then(context => {
                         var data = context.PersistenceData as BunkerGame.Workflows.GameData;
                         
                         // ЗАЩИТА ОТ NULL
                         if (data != null)
                         {
                             data.CurrentPlayerTurnIndex++;
                         }
                         
                         return ExecutionResult.Next();
                    })
                )

                // 3. ФАЗА ГОЛОСОВАНИЯ
                .Then<AnnounceVotingStep>()
                
                .While(data => data.CurrentRoundVotes.Count < data.PlayersCount)
                .Do(voteFlow => voteFlow
                    .WaitFor("PlayerVoted", data => data.GameId.ToString())
                    .Output((step, data) => {
                        // ИСПРАВЛЕНИЕ: Используем VoteEvent вместо Tuple
                        if (step.EventData is VoteEvent voteEvent)
                        {
                            if (data.CurrentRoundVotes == null) 
                                data.CurrentRoundVotes = new Dictionary<Guid, List<Guid>>();

                            data.CurrentRoundVotes[voteEvent.UserId] = voteEvent.TargetIds;
                        }
                        // Для надежности пробуем десериализовать, если пришел JsonElement (бывает в In-Memory)
                        else if (step.EventData is System.Text.Json.JsonElement json)
                        {
                            try 
                            {
                                var evt = System.Text.Json.JsonSerializer.Deserialize<VoteEvent>(json.GetRawText());
                                if (evt != null)
                                {
                                    if (data.CurrentRoundVotes == null) 
                                        data.CurrentRoundVotes = new Dictionary<Guid, List<Guid>>();
                                    data.CurrentRoundVotes[evt.UserId] = evt.TargetIds;
                                }
                            }
                            catch {}
                        }
                    })
                )
                
                // 4. Подсчет итогов раунда и переход этапа
                .Then<ProcessVotesStep>()
            )
            
            // --- КОНЕЦ ИГРЫ ---
            .Then<EndGameStep>();
    }
}