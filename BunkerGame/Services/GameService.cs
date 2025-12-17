using System.Collections.Concurrent;
using BunkerGame.Data;
using BunkerGame.DTOs.Game;
using BunkerGame.Models;
using Microsoft.EntityFrameworkCore;
// Workflow namespace нам больше не нужны для логики, но оставим, чтобы не ломать DI в Program.cs
using WorkflowCore.Interface;

namespace BunkerGame.Services;

public class GameService : IGameService
{
    private readonly ApplicationDbContext _context;
    // Оставляем зависимости, чтобы не ломать конструктор, но использовать не будем
    private readonly IWorkflowController _workflowController; 
    private readonly IPersistenceProvider _persistenceProvider;

    // --- ВРЕМЕННОЕ ХРАНИЛИЩЕ ДЛЯ ГОЛОСОВ (В ПАМЯТИ) ---
    // GameId -> HashSet<PlayerId> (кто уже проголосовал в текущем раунде)
    private static readonly ConcurrentDictionary<Guid, HashSet<Guid>> _votesInMemory = new();

    // Словари данных (без изменений)
    private readonly string[] _professions = { "Врач", "Инженер", "Солдат", "Учитель", "Повар", "Программист", "Плотник", "Юрист" };
    private readonly string[] _physiologyConditions = { "Идеально здоров", "Астма", "Онкология (1 стадия)", "Бесплодие", "Аллергия на пыль", "Толстый", "Атлет" };
    private readonly string[] _psychologies = { "Клаустрофобия", "Арахнофобия", "Депрессия", "Биполярное расстройство", "Психически здоров", "Паранойя" };
    private readonly string[] _hobbies = { "Футбол", "Садоводство", "Шахматы", "Стрельба", "Алкоголизм", "Вышивание", "Охота" };
    private readonly string[] _traits = { "Лидер", "Эгоист", "Паникер", "Добрый", "Лжец", "Конфликтный", "Харизматичный" };
    private readonly string[] _inventories = { "Аптечка", "Фонарик", "Пистолет (1 патрон)", "Карты", "Бутылка воды", "Нож", "Рация" };
    private readonly string[] _specialSkills = { "Взлом замков", "Первая помощь", "Стрельба навскидку", "Готовка из ничего", "Убеждение", "Ремонт техники" };
    private readonly string[] _additionalInfos = { "Родственник мэра", "Знает код от бункера", "Был в тюрьме", "Скрывает укус зомби", "Выиграл в лотерею", "Бесплоден" };

    public GameService(ApplicationDbContext context, IWorkflowController workflowController, IPersistenceProvider persistenceProvider)
    {
        _context = context;
        _workflowController = workflowController;
        _persistenceProvider = persistenceProvider;
    }

    public async Task<Guid> StartGameAsync(Guid roomId, Guid userId)
    {
        var room = await _context.Rooms
            .Include(r => r.Players) 
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null) throw new Exception("Room not found");
        if (room.HostId != userId) throw new InvalidOperationException("Запускать игру может только хост.");
        
        var game = new Game
        {
            Id = Guid.NewGuid(),
            HostId = userId,
            Phase = "Initialization",
            StartedAt = DateTime.UtcNow,
            AdditionalRounds = 0 
        };
        _context.Games.Add(game);

        var random = new Random();
        
        // Сортируем игроков, чтобы порядок был стабильным
        var sortedUsers = room.Players.OrderBy(u => u.Name).ToList();

        foreach (var user in sortedUsers)
        {
            var player = new Player
            {
                UserId = user.Id,
                GameId = game.Id,
                TotalScore = 0,
                Characteristics = GenerateCharacteristics(random)
            };
            _context.Players.Add(player);

            user.CurrentRoomId = null;
            user.CurrentGame = game;
            user.CurrentPlayerCharacter = player;
        }

        // --- ЛОГИКА ИНИЦИАЛИЗАЦИИ ВРУЧНУЮ ---
        game.AvailablePlaces = sortedUsers.Count / 2;
        if (game.AvailablePlaces == 0) game.AvailablePlaces = 1;
        
        game.Phase = "Opening";
        game.CurrentRoundNumber = 1; 
        
        // Назначаем ход первому игроку
        // Важно: Players еще не в БД, берем из sortedUsers и ищем соответствие, 
        // но проще сохранить сейчас, а потом достать ID.
        await _context.SaveChangesAsync();
        
        // Берем первого созданного игрока
        var firstPlayer = await _context.Players
            .Where(p => p.GameId == game.Id)
            .OrderBy(p => p.Id) // Сортировка по ID
            .FirstOrDefaultAsync();

        if (firstPlayer != null)
        {
            game.CurrentTurnPlayerId = firstPlayer.Id;
        }

        // Удаляем комнату
        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();
        
        // Инициализируем хранилище голосов
        _votesInMemory.TryAdd(game.Id, new HashSet<Guid>());

        // Workflow НЕ ЗАПУСКАЕМ. Мы управляем всем сами.
        return game.Id;
    }

    public async Task RevealTraitAsync(Guid gameId, Guid userId, string traitCode)
    {
        var game = await _context.Games
            .Include(g => g.Players)
            .FirstOrDefaultAsync(g => g.Id == gameId);
            
        if (game == null) throw new Exception("Game not found");
        
        if (game.Phase != "Opening") 
            throw new InvalidOperationException($"Нельзя открывать карты в фазе {game.Phase}.");

        var player = game.Players.FirstOrDefault(p => p.UserId == userId);
        if (player == null) throw new Exception("Player not found");

        if (game.CurrentTurnPlayerId != player.Id)
            throw new InvalidOperationException("Сейчас не ваш ход!");

        // 1. Открываем карту
        var characteristic = player.Characteristics.FirstOrDefault(c => c.Code == traitCode);
        if (characteristic == null) throw new InvalidOperationException($"Invalid trait code: {traitCode}");

        if (!characteristic.IsOpen)
        {
            characteristic.IsOpen = true;
            if (player.RevealedTraitKeys == null) player.RevealedTraitKeys = new List<string>();
            player.RevealedTraitKeys.Add(traitCode); // для совместимости
            _context.Entry(player).Property(p => p.Characteristics).IsModified = true;
        }

        // 2. ПЕРЕДАЕМ ХОД СЛЕДУЮЩЕМУ (Логика Workflows перенесена сюда)
        var allPlayers = game.Players.OrderBy(p => p.Id).ToList();
        var myIndex = allPlayers.FindIndex(p => p.Id == player.Id);
        
        // Следующий по кругу
        var nextIndex = (myIndex + 1) % allPlayers.Count;
        var nextPlayer = allPlayers[nextIndex];
        
        game.CurrentTurnPlayerId = nextPlayer.Id;

        // 3. ПРОВЕРЯЕМ, ЗАКОНЧИЛСЯ ЛИ РАУНД (Круг)
        // Если ход перешел снова к первому игроку (индекс 0), значит круг замкнулся
        if (nextIndex == 0)
        {
            // Увеличиваем раунд
            game.CurrentRoundNumber++;

            // --- УПРОЩЕННАЯ ЛОГИКА ДЛЯ ПРЕЗЕНТАЦИИ ---
            // Если прошло 2 раунда -> Голосование
            // Можешь поменять число 2 на любое другое.
            if (game.CurrentRoundNumber > 2) 
            {
                game.Phase = "Voting";
                game.CurrentTurnPlayerId = null; // В голосовании ходов нет
                
                // Очищаем голоса для нового этапа
                if (_votesInMemory.ContainsKey(game.Id))
                    _votesInMemory[game.Id].Clear();
                else
                    _votesInMemory.TryAdd(game.Id, new HashSet<Guid>());
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task VoteAsync(Guid gameId, Guid userId, Guid targetPlayerId)
    {
        var game = await _context.Games.Include(g => g.Players).FirstOrDefaultAsync(g => g.Id == gameId);
        if (game == null) throw new Exception("Game not found");

        if (game.Phase != "Voting")
            throw new InvalidOperationException("Голосование сейчас не идет.");

        var voter = game.Players.FirstOrDefault(p => p.UserId == userId);
        if (voter == null) throw new Exception("You are not in this game");
        if (voter.Id == targetPlayerId) throw new InvalidOperationException("Нельзя голосовать за себя.");

        // 1. Проверяем, голосовал ли уже (в памяти)
        var votes = _votesInMemory.GetOrAdd(game.Id, new HashSet<Guid>());
        if (votes.Contains(voter.Id))
        {
            throw new InvalidOperationException("Вы уже проголосовали.");
        }

        // 2. Начисляем баллы
        var target = game.Players.FirstOrDefault(p => p.Id == targetPlayerId);
        if (target != null)
        {
            target.TotalScore += 1; // Упростили: 1 голос = 1 балл (для демки сойдет)
        }

        // 3. Фиксируем голос
        votes.Add(voter.Id);

        // 4. Проверяем, все ли проголосовали
        if (votes.Count >= game.Players.Count)
        {
            // ВСЕ ПРОГОЛОСОВАЛИ -> ПЕРЕХОД ДАЛЬШЕ
            
            // Вариант для демки: После голосования сразу КОНЕЦ ИГРЫ
            // Если нужно несколько этапов, тут нужно написать if (game.CurrentRoundNumber > ...)
            
            game.Phase = "GameOver";
            game.CurrentTurnPlayerId = null;

            // Можно открыть все карты всем
            foreach (var p in game.Players)
            {
                foreach (var c in p.Characteristics) c.IsOpen = true;
                _context.Entry(p).Property(x => x.Characteristics).IsModified = true;
            }
            
            // Очистка памяти
            _votesInMemory.TryRemove(game.Id, out _);
        }

        await _context.SaveChangesAsync();
    }

    // --- ОСТАЛЬНЫЕ МЕТОДЫ БЕЗ ИЗМЕНЕНИЙ ---

    public async Task<object> GetGameStateForUserAsync(Guid gameId, Guid userId)
    {
        var game = await _context.Games
            .Include(g => g.Players)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null) return null!;

        var myPlayer = game.Players.FirstOrDefault(p => p.UserId == userId);
        var myPlayerId = myPlayer?.Id;

        // Определяем состояние IsOpen для Game Over (показать всё)
        bool isGameOver = game.Phase == "GameOver";

        var playersDto = game.Players.Select(p => {
            bool isMe = p.UserId == userId;

            var visibleChars = p.Characteristics.Select(c => new
            {
                Code = c.Code,
                Label = c.Label,
                // Если я, или карта открыта, или конец игры -> показываем
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
                Characteristics = visibleChars
            };
        });

        bool yourTurnNow = false;
        if (game.Phase == "Opening" && game.CurrentTurnPlayerId != null && myPlayerId != null)
        {
            yourTurnNow = game.CurrentTurnPlayerId == myPlayerId;
        }

        // Вычисляем победителей для фронта, если конец
        List<Guid> winnerIds = new();
        if (isGameOver)
        {
            winnerIds = game.Players
                .OrderByDescending(p => p.TotalScore)
                .Take(game.AvailablePlaces)
                .Select(p => p.Id)
                .ToList();
        }

        return new
        {
            GameId = game.Id,
            Phase = game.Phase,
            CurrentRound = game.CurrentRoundNumber,
            AvailablePlaces = game.AvailablePlaces,
            CurrentTurnPlayerId = game.CurrentTurnPlayerId,
            YourTurnNow = yourTurnNow,
            WinnerIds = winnerIds, // Можно добавить в DTO для удобства
            Players = playersDto
        };
    }
    
    public async Task<List<GameDto>> GetAllGamesAsync()
    {
        return await _context.Games
            .Select(g => new GameDto
            {
                Id = g.Id,
                CurrentStep = g.Phase,
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
        
        _context.Games.Remove(game);
        // Чистим память
        _votesInMemory.TryRemove(gameId, out _);
        
        await _context.SaveChangesAsync();
        return true;
    }

    private List<PlayerCharacteristic> GenerateCharacteristics(Random random)
    {
        var age = random.Next(18, 90);
        var health = _physiologyConditions[random.Next(_physiologyConditions.Length)];

        return new List<PlayerCharacteristic>
        {
            new() { Code = "profession", Label = "Профессия", Value = _professions[random.Next(_professions.Length)] },
            new() { Code = "physiology", Label = "Биология", Value = $"{age} лет, {health}" },
            new() { Code = "psychology", Label = "Психология", Value = _psychologies[random.Next(_psychologies.Length)] },
            new() { Code = "gender", Label = "Пол", Value = random.Next(0, 2) == 0 ? "Мужчина" : "Женщина" },
            new() { Code = "inventory", Label = "Инвентарь", Value = _inventories[random.Next(_inventories.Length)] },
            new() { Code = "hobby", Label = "Хобби", Value = _hobbies[random.Next(_hobbies.Length)] },
            new() { Code = "specialSkill", Label = "Особый навык", Value = _specialSkills[random.Next(_specialSkills.Length)] },
            new() { Code = "characterTrait", Label = "Черта характера", Value = _traits[random.Next(_traits.Length)] },
            new() { Code = "additionalInfo", Label = "Доп. информация", Value = _additionalInfos[random.Next(_additionalInfos.Length)] }
        };
    }
}