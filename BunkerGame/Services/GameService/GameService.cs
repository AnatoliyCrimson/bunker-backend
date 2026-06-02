using BunkerGame.Data;
using BunkerGame.Models;
using BunkerGame.DTOs.Game;
using BunkerGame.Models.GameModels;
using BunkerGame.Services.AiService;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using BunkerGame.Hubs;

namespace BunkerGame.Services.GameService;

public class GameService : IGameService
{
    private readonly ApplicationDbContext _context;
    private readonly IAiService _aiService;
    private readonly IHubContext<GameHub> _hubContext;

    public GameService(ApplicationDbContext context, IAiService aiService, IHubContext<GameHub> hubContext)
    {
        _context = context;
        _aiService = aiService;
        _hubContext = hubContext;
    }
    
    public async Task<Guid> StartGameAsync(Guid userId, int additionalRounds)
    {
        var hostUser = await _context.Users.FindAsync(userId);
        if (hostUser == null) throw new Exception("User not found");

        if (hostUser.CurrentRoomId == null)
        {
            throw new InvalidOperationException("You are not in a room.");
        }

        var room = await _context.Rooms
            .Include(r => r.Users)
            .Include(r => r.Game)
            .FirstOrDefaultAsync(r => r.Id == hostUser.CurrentRoomId);

        if (room == null) throw new Exception("Room not found");

        if (room.HostId != userId)
        {
            throw new InvalidOperationException("Only the host can start the game.");
        }
        if (room.Users.Count < 4) throw new InvalidOperationException("Для начала игры нужно минимум 4 игрока.");
        
        // Генерируем историю через ИИ
        var aiStory = await _aiService.GenerateInitialStoryAsync(room.Users.Count, room.Users.Count / 2);
        if (aiStory == null)
        {
            throw new Exception("Сервер генерации перегружен. Попробуйте начать игру позже.");
        }

        var sortedUsers = room.Users.OrderBy(u => u.Id).ToList();
        
        var game = new Game
        {
            Id = Guid.NewGuid(),
            HostId = userId,
            Phase = GamePhase.Story, // Начинаем с фазы Story
            CurrentStage = 1,
            CurrentRoundNumber = 1,
            RoomId = room.Id,
            StartedAt = DateTime.UtcNow,
            AdditionalRounds = additionalRounds,
            AvailablePlaces =  room.Users.Count/2,
            DisasterName = aiStory.DisasterName,
            DisasterDescription = aiStory.DisasterDescription,
            BunkerDescription = aiStory.BunkerDescription,
            BunkerRooms = aiStory.Rooms.Select(r => new BunkerGame.Models.GameModels.BunkerRoom
            {
                Name = r.Name,
                Status = r.Status,
                Description = r.Description
            }).ToList()
        };
        _context.Games.Add(game);
        
        for (int i = 0; i < sortedUsers.Count; i++)
        {
            var user = sortedUsers[i];
            var aiPlayer = aiStory.Players[i];

            var player = new Player
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                GameId = game.Id,
                TotalScore = 0,
                Characteristics = new List<PlayerCharacteristic>
                {
                    new() { Code = "bio", Label = "Био", Value = aiPlayer.Bio },
                    new() { Code = "health", Label = "Здоровье", Value = aiPlayer.Health },
                    new() { Code = "profession", Label = "Профессия", Value = aiPlayer.Profession },
                    new() { Code = "character", Label = "Характер", Value = aiPlayer.Character },
                    new() { Code = "hobby", Label = "Хобби", Value = aiPlayer.Hobby },
                    new() { Code = "phobia", Label = "Фобия", Value = aiPlayer.Phobia },
                    new() { Code = "inventory", Label = "Инвентарь", Value = aiPlayer.Inventory },
                    new() { Code = "knowledge", Label = "Знание", Value = aiPlayer.Knowledge },
                    new() { Code = "info", Label = "Доп. инф.", Value = aiPlayer.Info }
                }
            };
            
            _context.Players.Add(player);
            user.CurrentGame = game;
            user.CurrentPlayerCharacter = player;
        }
            
        // Назначаем первого игрока для хода (когда дойдет до фазы Turn)
        var firstPlayer = await _context.Players.FirstOrDefaultAsync(p => p.GameId == game.Id);
        game.CurrentTurnPlayerId = firstPlayer?.Id;
        
        room.IsGameStart = true;
        
        await _context.SaveChangesAsync();
        
        // Уведомляем участников комнаты, что игра началась
        await _hubContext.Clients.Group(room.Id.ToString()).SendAsync("GameStarted", game.Id);
        
        return game.Id;
    }

    public async Task EndStoryPhaseAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) throw new Exception("User not found");

        if (user.CurrentGameId == null)
            throw new InvalidOperationException("Вы не в игре.");

        var game = await _context.Games
            .Include(g => g.Players)
            .FirstOrDefaultAsync(g => g.Id == user.CurrentGameId);

        if (game == null) throw new Exception("Игра не найдена.");

        if (game.HostId != userId)
            throw new InvalidOperationException("Только хост может завершить фазу истории.");

        if (game.Phase != GamePhase.Story)
            throw new InvalidOperationException("Игра не находится в фазе истории.");

        // Переходим к фазе хода
        game.Phase = GamePhase.Turn;
        
        // Убеждаемся, что первый игрок назначен
        if (game.CurrentTurnPlayerId == null)
        {
            var firstPlayer = game.Players.OrderBy(p => p.UserId).FirstOrDefault();
            game.CurrentTurnPlayerId = firstPlayer?.Id;
        }

        _context.Games.Update(game);
        await _context.SaveChangesAsync();
        
        // Уведомляем участников о смене фазы
        await _hubContext.Clients.Group(game.RoomId.ToString()).SendAsync("GameUpdated");
    }
    
    public async Task PresentationTrait(Guid userId, string traitCode)
    {
        var user = await _context.Users
            .Include(u => u.CurrentPlayerCharacter)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.CurrentPlayerCharacter == null)
            throw new Exception("Игрок не найден в этой игре.");

        var player = user.CurrentPlayerCharacter;

        var game = await _context.Games
            .Include(g => g.Players)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(g => g.Id == player.GameId);

        if (game == null)
            throw new Exception("Игра не найдена.");

        if (game.Phase != GamePhase.Turn)
            throw new InvalidOperationException("Сейчас не фаза открытия характеристик.");

        if (game.CurrentTurnPlayerId != player.Id)
            throw new InvalidOperationException("Сейчас не ваш ход.");

        // Находим характеристику
        var characteristic = player.Characteristics.FirstOrDefault(c => c.Code == traitCode);
        if (characteristic == null)
            throw new Exception("Характеристика не найдена.");

        if (characteristic.IsOpen)
            throw new InvalidOperationException("Эта характеристика уже открыта.");

        // Открываем характеристику
        characteristic.IsOpen = true;

        // Лог события
        var log = new GameEventLog
        {
            GameId = game.Id,
            Description = $"Игрок {user.Name} открыл характеристику {characteristic.Label} этап {game.CurrentStage} раунд {game.CurrentRoundNumber}"
        };
        _context.GameEventLogs.Add(log);
        
        // Переназначаем списки, чтобы EF Core точно отследил изменения в JSONB колонках
        player.Characteristics = new List<PlayerCharacteristic>(player.Characteristics);
        
        // Добавляем в список открытых (если это нужно для фронтенда/истории)
        if (!player.PresentationTraitKeys.Contains(traitCode))
        {
            player.PresentationTraitKeys.Add(traitCode);
            player.PresentationTraitKeys = new List<string>(player.PresentationTraitKeys);
        }

        // Передаем ход следующему игроку
        var sortedPlayers = game.Players.OrderBy(p => p.UserId).ToList(); // Сортировка по UserId, как при старте
        var currentPlayerIndex = sortedPlayers.FindIndex(p => p.Id == player.Id);
        var nextPlayerIndex = currentPlayerIndex + 1;

        if (nextPlayerIndex < sortedPlayers.Count)
        {
            // Круг продолжается
            game.CurrentTurnPlayerId = sortedPlayers[nextPlayerIndex].Id;
        }
        else
        {
            // Круг завершен, все игроки сделали ход
            game.CurrentTurnPlayerId = sortedPlayers[0].Id; // Возвращаем ход первому игроку для следующего раунда

            // Проверяем, закончились ли раунды в текущем этапе
            int maxRoundsInStage = GetMaxRoundsForStage(game.CurrentStage, game.AdditionalRounds);

            if (game.CurrentRoundNumber < maxRoundsInStage)
            {
                // Начинаем следующий раунд в текущем этапе
                game.CurrentRoundNumber++;
            }
            else
            {
                // Раунды этапа закончились, переходим к обсуждению
                game.Phase = GamePhase.Discussion;
                // Устанавливаем время окончания обсуждения (например, 5 минут)
                game.DiscussionEndsAt = DateTime.UtcNow.AddMinutes(1);
            }
        }

        // EF Core отслеживает изменения в JSONB, если мы переназначаем свойство или используем методы, 
        // но для надежности можно вызвать Update
        _context.Players.Update(player);
        _context.Games.Update(game);
        
        await _context.SaveChangesAsync();
        
        // Уведомляем участников об открытии характеристики и смене хода
        Console.WriteLine($"[DEBUG SignalR] Game: {game.Id}, Room: {game.RoomId}. Sending GameUpdated.");
        await _hubContext.Clients.Group(game.RoomId.ToString()).SendAsync("GameUpdated");
        Console.WriteLine($"[DEBUG SignalR] SENT to group {game.RoomId}");
    }

    public async Task EndDiscussion(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) throw new Exception("User not found");

        if (user.CurrentGameId == null)
        {
            throw new InvalidOperationException("You are not in an active game.");
        }

        var game = await _context.Games
            .FirstOrDefaultAsync(g => g.Id == user.CurrentGameId);

        if (game == null)
            throw new Exception("Game not found.");

        if (game.HostId != userId)
            throw new InvalidOperationException("Только хост может завершить обсуждение.");

        if (game.Phase != GamePhase.Discussion)
            throw new InvalidOperationException("Сейчас не фаза обсуждения.");

        // Проверяем, истекло ли время обсуждения
        if (game.DiscussionEndsAt.HasValue && game.DiscussionEndsAt.Value > DateTime.UtcNow)
        {
            var remainingTime = game.DiscussionEndsAt.Value - DateTime.UtcNow;
            throw new InvalidOperationException($"Время на обсуждение еще не вышло. Осталось: {remainingTime.Minutes} мин {remainingTime.Seconds} сек.");
        }
        
        // Переводим игру в фазу голосования
        game.Phase = GamePhase.Voting;
        
        // Очищаем голоса (на всякий случай, хотя они должны быть пустыми)
        game.CurrentVotes = new Dictionary<Guid, List<Guid>>();
        game.DiscussionEndsAt = null;

        _context.Games.Update(game);
        await _context.SaveChangesAsync();
        
        // Уведомляем участников о переходе к голосованию
        await _hubContext.Clients.Group(game.RoomId.ToString()).SendAsync("GameUpdated");
    }
    
    public async Task SubmitVote(Guid userId, List<Guid> votedPlayerIds)
    {
        var user = await _context.Users
            .Include(u => u.CurrentPlayerCharacter)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.CurrentPlayerCharacter == null)
            throw new Exception("Player not found or not in game.");

        if (user.CurrentGameId == null)
        {
            throw new InvalidOperationException("You are not in an active game.");
        }

        var player = user.CurrentPlayerCharacter;

        var game = await _context.Games
            .Include(g => g.Players)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(g => g.Id == user.CurrentGameId);

        if (game == null)
            throw new Exception("Game not found.");

        if (game.Phase != GamePhase.Voting)
            throw new InvalidOperationException("Сейчас не фаза голосования.");

        if (game.CurrentVotes.ContainsKey(player.Id))
            throw new InvalidOperationException("Вы уже проголосовали.");

        int requiredVotes = game.AvailablePlaces - 1;
        if (votedPlayerIds.Count != requiredVotes)
            throw new InvalidOperationException($"Вы должны проголосовать ровно за {requiredVotes} игроков.");

        if (votedPlayerIds.Contains(player.Id))
            throw new InvalidOperationException("Нельзя голосовать за себя.");

        if (votedPlayerIds.Distinct().Count() != votedPlayerIds.Count)
            throw new InvalidOperationException("Голоса должны быть за разных игроков.");

        // Лог события
        var votedPlayerNames = game.Players
            .Where(p => votedPlayerIds.Contains(p.Id))
            .Select(p => p.User?.Name ?? "Неизвестный")
            .ToList();
            
        var log = new GameEventLog
        {
            GameId = game.Id,
            Description = $"Игрок {user.Name} проголосовал за {string.Join(", ", votedPlayerNames)} этап {game.CurrentStage} раунд {game.CurrentRoundNumber}"
        };
        _context.GameEventLogs.Add(log);

        // Сохраняем голос
        game.CurrentVotes[player.Id] = votedPlayerIds;
        player.IsVoted = true;

        // EF Core отслеживает изменения в JSONB, если мы переназначаем свойство
        game.CurrentVotes = new Dictionary<Guid, List<Guid>>(game.CurrentVotes);
        _context.Games.Update(game);
        _context.Players.Update(player);
        await _context.SaveChangesAsync();
        
        // Уведомляем участников, что кто-то проголосовал (для обновления галочек/статуса)
        await _hubContext.Clients.Group(game.RoomId.ToString()).SendAsync("GameUpdated");
        
        // Проверяем, все ли проголосовали
        if (game.CurrentVotes.Count == game.Players.Count)
        {
            await ProcessVotingResults(game);
        }
    }
    
    private async Task ProcessVotingResults(Game game)
    {
        int voteWeight = GetVoteWeight(game.CurrentStage);

        // Начисляем баллы
        foreach (var vote in game.CurrentVotes)
        {
            foreach (var targetPlayerId in vote.Value)
            {
                var targetPlayer = game.Players.FirstOrDefault(p => p.Id == targetPlayerId);
                if (targetPlayer != null)
                {
                    targetPlayer.TotalScore += voteWeight;
                }
            }
        }

        // Очищаем голоса и флаги
        game.CurrentVotes = new Dictionary<Guid, List<Guid>>();
        foreach (var p in game.Players)
        {
            p.IsVoted = false;
        }

        // Проверяем, есть ли еще этапы
        int totalStages = 3 + (game.AdditionalRounds > 0 ? 1 : 0); // 3 основных + 1 этап для доп раундов

        if (game.CurrentStage < totalStages)
        {
            // Переход к следующему этапу
            game.CurrentStage++;
            game.CurrentRoundNumber = 1;
            game.Phase = GamePhase.Turn;
            
            // Ход передается первому игроку
            var sortedPlayers = game.Players.OrderBy(p => p.UserId).ToList();
            game.CurrentTurnPlayerId = sortedPlayers.First().Id;
        }
        else
        {
            // Игра окончена
            game.Phase = GamePhase.End;
            
            // Открываем все оставшиеся характеристики
            foreach (var p in game.Players)
            {
                foreach (var c in p.Characteristics)
                {
                    c.IsOpen = true;
                }
                p.Characteristics = new List<PlayerCharacteristic>(p.Characteristics);
            }

            // Генерируем финальный вердикт через ИИ
            try 
            {
                var logs = await _context.GameEventLogs
                    .Where(l => l.GameId == game.Id)
                    .OrderBy(l => l.Timestamp)
                    .ToListAsync();

                var verdict = await _aiService.GenerateFinalVerdictAsync(game, logs);
                if (verdict != null)
                {
                    game.AchievementVerdict = System.Text.Json.JsonSerializer.Serialize(verdict, new System.Text.Json.JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase 
                    });
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не прерываем завершение игры
                // TODO: Добавить настоящий логгер в класс
            }
        }

        _context.Games.Update(game);
        await _context.SaveChangesAsync();
        
        // Уведомляем участников о результатах голосования (след. этап или финал)
        await _hubContext.Clients.Group(game.RoomId.ToString()).SendAsync("GameUpdated");
    }
    
    private int GetVoteWeight(int stage)
    {
        if (stage == 1) return 1;
        if (stage == 2) return 2;
        if (stage >= 3) return 3;
        return 1;
    }
    
    private int GetMaxRoundsForStage(int stage, int additionalRounds)
    {
        // Логика количества раундов:
        // Этап 1: 3 раунда
        // Этап 2: 2 раунда
        // Этап 3: 1 раунд
        // Дополнительные раунды (если есть) можно распределить или добавить в конец.
        // Пока сделаем простую логику:
        if (stage == 1) return 3;
        if (stage == 2) return 2;
        if (stage == 3) return 1;
        // TODO: добавить логику для доп раундов
        // Если этапов больше (например, из-за дополнительных раундов), 
        // пусть будет по 1 раунду на каждый следующий этап.
        return 1;
    }
    
    public async Task<object> GetGameStateAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.CurrentGameId == null) return null!;

        var game = await _context.Games
            .Include(g => g.Players)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(g => g.Id == user.CurrentGameId);

        if (game == null) return null!;

        var myPlayer = game.Players.FirstOrDefault(p => p.UserId == userId);
        var myPlayerId = myPlayer?.Id;

        bool isGameOver = game.Phase == GamePhase.End;

        var playersDto = game.Players.Select(p => {
            bool isMe = p.UserId == userId;

            var visibleChars = p.Characteristics.Select(c => new
            {
                Code = c.Code,
                Label = c.Label,
                Value = (isMe || c.IsOpen || isGameOver) ? c.Value : "???",
                IsOpen = c.IsOpen || isGameOver
            }).ToList();

            return new
            {
                Id = p.Id,
                UserId = p.UserId,
                Name = p.User?.Name ?? "Unknown",
                AvatarUrl = p.User?.AvatarUrl,
                IsMe = isMe,
                TotalScore = p.TotalScore,
                IsVoted = p.IsVoted,
                Characteristics = visibleChars
            };
        }).ToList();

        bool yourTurnNow = false;
        if (game.Phase == GamePhase.Turn && game.CurrentTurnPlayerId != null && myPlayerId != null)
        {
            yourTurnNow = game.CurrentTurnPlayerId == myPlayerId;
        }

        return new
        {
            GameId = game.Id,
            RoomId = game.RoomId,
            HostId = game.HostId,
            Phase = game.Phase.ToString(),
            CurrentStage = game.CurrentStage,
            CurrentRound = game.CurrentRoundNumber,
            AdditionalRounds = game.AdditionalRounds,
            AvailablePlaces = game.AvailablePlaces,
            CurrentTurnPlayerId = game.CurrentTurnPlayerId,
            YourTurnNow = yourTurnNow,
            DiscussionEndsAt = game.DiscussionEndsAt,
            DisasterName = game.DisasterName,
            DisasterDescription = game.DisasterDescription,
            BunkerDescription = game.BunkerDescription,
            BunkerRooms = game.BunkerRooms,
            Players = playersDto,
            AchievementVerdict = string.IsNullOrEmpty(game.AchievementVerdict) 
                ? null 
                : System.Text.Json.JsonSerializer.Deserialize<object>(game.AchievementVerdict)
        };
    }

    public async Task<List<GameDto>> GetAllGamesAsync()
    {
        return await _context.Games
            .Select(g => new GameDto
            {
                Id = g.Id,
                CurrentStep = g.Phase.ToString(),
                PlayerCount = g.Players.Count,
                StartedAt = g.StartedAt
            })
            .OrderByDescending(g => g.StartedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteGameAsync(Guid gameId, Guid userId)
    {
        var game = await _context.Games.FindAsync(gameId);
        if (game == null) return false;
        if (game.HostId != userId) throw new InvalidOperationException("Удалять игру может только хост.");

        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == game.RoomId);
        if (room != null)
        {
            room.IsGameStart = false;
        }
        
        var roomId = game.RoomId;
        _context.Games.Remove(game);
        await _context.SaveChangesAsync();
        
        // Уведомляем участников, что игра удалена/завершена
        await _hubContext.Clients.Group(roomId.ToString()).SendAsync("GameDeleted");
        
        return true;
    }
}