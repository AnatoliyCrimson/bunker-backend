using BunkerGame.Data;
using BunkerGame.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace BunkerGame.Workflows.Steps;

public class ProcessVotesStep : IStepBody
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<GameHub> _hubContext;

    public ProcessVotesStep(ApplicationDbContext context, IHubContext<GameHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = context.PersistenceData as GameData;
        var currentStage = data.Stages[data.CurrentStageIndex];
        int weight = currentStage.VoteWeight;

        // 1. Загружаем игроков для обновления очков
        var game = await _context.Games.Include(g => g.Players).FirstOrDefaultAsync(g => g.Id == data.GameId);
        if (game == null) return ExecutionResult.Next();

        // 2. Подсчет голосов
        // data.CurrentRoundVotes это Dictionary<UserId, List<TargetId>>
        var roundScores = new Dictionary<Guid, int>(); // UserId -> Полученные баллы

        foreach (var voteEntry in data.CurrentRoundVotes)
        {
            // voteEntry.Value - список ID, за кого проголосовали
            foreach (var targetId in voteEntry.Value)
            {
                if (!roundScores.ContainsKey(targetId)) roundScores[targetId] = 0;
                roundScores[targetId] += weight; // Добавляем вес (1, 2 или 3)
            }
        }

        // 3. Сохраняем в БД
        foreach (var player in game.Players)
        {
            if (roundScores.TryGetValue(player.UserId, out int score))
            {
                player.VoteCount += score; // Накапливаем общий счет
            }
        }
        await _context.SaveChangesAsync();

        // 4. Отправляем результаты клиентам
        await _hubContext.Clients.Group(data.GameId.ToString()).SendAsync("RoundResults", new
        {
            stage = currentStage.Name,
            round = data.RoundsPlayedInCurrentStage + 1,
            scores = roundScores, // Кто сколько получил в этом раунде
            totalScores = game.Players.Select(p => new { p.UserId, p.VoteCount }) // Общий счет
        });

        // 5. Очищаем буфер голосов для следующего раунда
        data.CurrentRoundVotes.Clear();

        // 6. Логика перехода этапов
        data.RoundsPlayedInCurrentStage++;
        if (data.RoundsPlayedInCurrentStage >= currentStage.RoundsCount)
        {
            data.CurrentStageIndex++;
            data.RoundsPlayedInCurrentStage = 0; // Сброс раундов для нового этапа
        }

        if (data.CurrentStageIndex >= data.Stages.Count)
        {
            data.IsGameOver = true;
        }

        return ExecutionResult.Next();
    }
}