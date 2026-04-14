using BunkerGame.Data;
using BunkerGame.Models;
using BunkerGame.DTOs.Game;
using Microsoft.EntityFrameworkCore;

namespace BunkerGame.Services;

public class GameService : IGameService
{
    
    private readonly string[] _professions = { "Врач", "Инженер", "Солдат", "Учитель", "Повар", "Программист", "Плотник", "Юрист" };
    private readonly string[] _physiologyConditions = { "Идеально здоров", "Астма", "Онкология (1 стадия)", "Бесплодие", "Аллергия на пыль", "Толстый", "Атлет" };
    private readonly string[] _psychologies = { "Клаустрофобия", "Арахнофобия", "Депрессия", "Биполярное расстройство", "Психически здоров", "Паранойя" };
    private readonly string[] _hobbies = { "Футбол", "Садоводство", "Шахматы", "Стрельба", "Алкоголизм", "Вышивание", "Охота" };
    private readonly string[] _traits = { "Лидер", "Эгоист", "Паникер", "Добрый", "Лжец", "Конфликтный", "Харизматичный" };
    private readonly string[] _inventories = { "Аптечка", "Фонарик", "Пистолет (1 патрон)", "Карты", "Бутылка воды", "Нож", "Рация" };
    private readonly string[] _specialSkills = { "Взлом замков", "Первая помощь", "Стрельба навскидку", "Готовка из ничего", "Убеждение", "Ремонт техники" };
    private readonly string[] _additionalInfos = { "Родственник мэра", "Знает код от бункера", "Был в тюрьме", "Скрывает укус зомби", "Выиграл в лотерею", "Бесплоден" };
    
    private readonly ApplicationDbContext _context;
    public GameService(ApplicationDbContext context)
    {
        _context = context;
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
        
        var sortedUsers = room.Users.OrderBy(u => u.Id).ToList();
        
        var game = new Game
        {
            Id = Guid.NewGuid(),
            HostId = userId,
            Phase = GamePhase.Init,
            CurrentStage = 1,
            CurrentRoundNumber = 1,
            RoomId = room.Id,
            StartedAt = DateTime.UtcNow,
            AdditionalRounds = additionalRounds,
            AvailablePlaces =  room.Users.Count/2
        };
        _context.Games.Add(game);
        
        var random = new Random();
        var players = new List<Player>();

        foreach (var user in sortedUsers)
        {
            var player = new Player
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                GameId = game.Id,
                TotalScore = 0,
                Characteristics = GenerateCharacteristics(random)
            };
            players.Add(player);
            _context.Players.Add(player);
            
            user.CurrentGame = game;
            user.CurrentPlayerCharacter = player;
        }
            
        game.CurrentTurnPlayerId = players.First().Id;
        
        room.IsGameStart = true;
        
        game.Phase = GamePhase.Turn;
        await _context.SaveChangesAsync();
        return game.Id;
    }

    private List<PlayerCharacteristic> GenerateCharacteristics(Random random)
    {
        var age = random.Next(18, 100);
        var health = _physiologyConditions[random.Next(_physiologyConditions.Length)];
        
        return new List<PlayerCharacteristic>
        {
            new() { Code = "bio", Label = "Пол", Value = $"{(random.Next(0, 2) == 0 ? "Мужчина" : "Женщина")}, {age} лет" } ,
            new() { Code = "health", Label = "Здоровье", Value = $"{health}" },
            new() { Code = "profession", Label = "Профессия", Value = _professions[random.Next(_professions.Length)] },
            new() { Code = "character", Label = "Характер", Value = _traits[random.Next(_traits.Length)] },
            new() { Code = "hobby", Label = "Хобби", Value = _hobbies[random.Next(_hobbies.Length)] },
            new() { Code = "phobia", Label = "Фобия", Value = _psychologies[random.Next(_psychologies.Length)] },
            new() { Code = "inventory", Label = "Инвентарь", Value = _inventories[random.Next(_inventories.Length)] },
            new() { Code = "knowledge", Label = "Знание", Value = _specialSkills[random.Next(_specialSkills.Length)] },
            new() { Code = "info", Label = "Доп. информация", Value = _additionalInfos[random.Next(_additionalInfos.Length)] }
        };
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
                game.DiscussionEndsAt = DateTime.UtcNow.AddMinutes(2);
            }
        }

        // EF Core отслеживает изменения в JSONB, если мы переназначаем свойство или используем методы, 
        // но для надежности можно вызвать Update
        _context.Players.Update(player);
        _context.Games.Update(game);
        
        await _context.SaveChangesAsync();
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

        // Сохраняем голос
        game.CurrentVotes[player.Id] = votedPlayerIds;
        player.IsVoted = true;

        // Проверяем, все ли проголосовали
        if (game.CurrentVotes.Count == game.Players.Count)
        {
            await ProcessVotingResults(game);
        }
        else
        {
            // EF Core отслеживает изменения в JSONB, если мы переназначаем свойство
            game.CurrentVotes = new Dictionary<Guid, List<Guid>>(game.CurrentVotes);
            _context.Games.Update(game);
            _context.Players.Update(player);
            await _context.SaveChangesAsync();
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
        }

        _context.Games.Update(game);
        await _context.SaveChangesAsync();
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
            HostId = game.HostId,
            Phase = game.Phase.ToString(),
            CurrentStage = game.CurrentStage,
            CurrentRound = game.CurrentRoundNumber,
            AvailablePlaces = game.AvailablePlaces,
            CurrentTurnPlayerId = game.CurrentTurnPlayerId,
            YourTurnNow = yourTurnNow,
            DiscussionEndsAt = game.DiscussionEndsAt,
            Players = playersDto
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

        _context.Games.Remove(game);
        await _context.SaveChangesAsync();
        return true;
    }
}