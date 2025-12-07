using BunkerGame.Data;
using BunkerGame.DTOs.Game;
using BunkerGame.Hubs;
using BunkerGame.Models;
using BunkerGame.Workflows;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;

namespace BunkerGame.Services;

public class GameService : IGameService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkflowController _workflowController;
    private readonly IPersistenceProvider _persistenceProvider;
    private readonly IHubContext<GameHub> _hubContext;

    // --- Словари для генерации ---
    private readonly string[] _professions = { "Врач", "Инженер", "Солдат", "Учитель", "Повар", "Программист", "Плотник", "Юрист" };
    private readonly string[] _physiologyConditions = { "Идеально здоров", "Астма", "Онкология (1 стадия)", "Бесплодие", "Аллергия на пыль", "Толстый", "Атлет" };
    private readonly string[] _psychologies = { "Клаустрофобия", "Арахнофобия", "Депрессия", "Биполярное расстройство", "Психически здоров", "Паранойя" };
    private readonly string[] _hobbies = { "Футбол", "Садоводство", "Шахматы", "Стрельба", "Алкоголизм", "Вышивание", "Охота" };
    private readonly string[] _traits = { "Лидер", "Эгоист", "Паникер", "Добрый", "Лжец", "Конфликтный", "Харизматичный" };
    private readonly string[] _inventories = { "Аптечка", "Фонарик", "Пистолет (1 патрон)", "Карты", "Бутылка воды", "Нож", "Рация" };
    private readonly string[] _specialSkills = { "Взлом замков", "Первая помощь", "Стрельба навскидку", "Готовка из ничего", "Убеждение", "Ремонт техники" };
    private readonly string[] _additionalInfos = { "Родственник мэра", "Знает код от бункера", "Был в тюрьме", "Скрывает укус зомби", "Выиграл в лотерею", "Бесплоден" };

    public GameService(
        ApplicationDbContext context, 
        IWorkflowController workflowController,
        IPersistenceProvider persistenceProvider,
        IHubContext<GameHub> hubContext)
    {
        _context = context;
        _workflowController = workflowController;
        _persistenceProvider = persistenceProvider;
        _hubContext = hubContext;
    }

    public async Task<Guid> StartGameAsync(Guid roomId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) throw new Exception("Room not found");
        
        var game = new Game
        {
            RoomId = roomId,
            PlayerIds = new List<Guid>(room.PlayerIds),
            CurrentStep = "Initialization" // Начальный статус
        };

        var random = new Random();
        foreach (var userId in room.PlayerIds)
        {
            var age = random.Next(18, 90);
            var health = _physiologyConditions[random.Next(_physiologyConditions.Length)];
            
            var player = new Player
            {
                UserId = userId,
                GameId = game.Id,
                Gender = random.Next(0, 2) == 0 ? "Мужчина" : "Женщина",
                Physiology = $"{age} лет, {health}",
                Psychology = _psychologies[random.Next(_psychologies.Length)],
                Profession = _professions[random.Next(_professions.Length)],
                Inventory = _inventories[random.Next(_inventories.Length)],
                Hobby = _hobbies[random.Next(_hobbies.Length)],
                SpecialSkill = _specialSkills[random.Next(_specialSkills.Length)],
                CharacterTrait = _traits[random.Next(_traits.Length)],
                AdditionalInfo = _additionalInfos[random.Next(_additionalInfos.Length)],
                IsKicked = false,
                VoteCount = 0
            };
            
            _context.Add(player);
            game.Players.Add(player);
        }

        // 1. СНАЧАЛА СОХРАНЯЕМ ИГРУ В БД
        _context.Games.Add(game);
        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync(); // <-- Теперь ID игры точно есть в базе

        // 2. ЗАПУСКАЕМ WORKFLOW
        // Используем полный путь к классу GameData, чтобы избежать путаницы
        var workflowId = await _workflowController.StartWorkflow("BunkerGameWorkflow", 1, new BunkerGame.Workflows.GameData { GameId = game.Id });
        
        // 3. ОБНОВЛЯЕМ ID WORKFLOW В ИГРЕ
        game.WorkflowInstanceId = Guid.Parse(workflowId);
        await _context.SaveChangesAsync();
        
        return game.Id;
    }

    public async Task<object> GetGameStateForUserAsync(Guid gameId, Guid userId)
    {
        var game = await _context.Games
            .Include(g => g.Players)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null) return null!;

        // Определение текущего хода
        Guid? activePlayerId = null;
        if (game.WorkflowInstanceId.HasValue)
        {
            try 
            {
                var wfInstance = await _persistenceProvider.GetWorkflowInstance(game.WorkflowInstanceId.Value.ToString());
                if (wfInstance != null && wfInstance.Data is GameData data)
                {
                    if (data.TurnOrder != null && 
                        data.CurrentPlayerTurnIndex >= 0 && 
                        data.CurrentPlayerTurnIndex < data.TurnOrder.Count)
                    {
                        activePlayerId = data.TurnOrder[data.CurrentPlayerTurnIndex];
                    }
                }
            }
            catch 
            {
                // Игнорируем ошибки чтения воркфлоу при простом получении стейта
            }
        }

        var playersDto = game.Players.Select(p => {
            bool isMe = p.UserId == userId;
            var revealed = p.RevealedTraitKeys ?? new List<string>();
            bool showAll = revealed.Contains("ALL"); 

            return new
            {
                Id = p.Id,
                UserId = p.UserId,
                Name = p.User?.UserName ?? "Unknown",
                AvatarUrl = p.User?.AvatarUrl,
                IsMe = isMe,
                NowTurnToOpen = (p.UserId == activePlayerId), // true, если сейчас его очередь

                // Характеристики
                Profession = (isMe || showAll || revealed.Contains(nameof(Player.Profession))) ? p.Profession : "???",
                Physiology = (isMe || showAll || revealed.Contains(nameof(Player.Physiology))) ? p.Physiology : "???",
                Psychology = (isMe || showAll || revealed.Contains(nameof(Player.Psychology))) ? p.Psychology : "???",
                Gender = (isMe || showAll || revealed.Contains(nameof(Player.Gender))) ? p.Gender : "???",
                Inventory = (isMe || showAll || revealed.Contains(nameof(Player.Inventory))) ? p.Inventory : "???",
                Hobby = (isMe || showAll || revealed.Contains(nameof(Player.Hobby))) ? p.Hobby : "???",
                SpecialSkill = (isMe || showAll || revealed.Contains(nameof(Player.SpecialSkill))) ? p.SpecialSkill : "???",
                CharacterTrait = (isMe || showAll || revealed.Contains(nameof(Player.CharacterTrait))) ? p.CharacterTrait : "???",
                AdditionalInfo = (isMe || showAll || revealed.Contains(nameof(Player.AdditionalInfo))) ? p.AdditionalInfo : "???",
                
                IsKicked = p.IsKicked,
                VoteCount = p.VoteCount,
                RevealedTraits = revealed
            };
        });

        return new
        {
            GameId = game.Id,
            CurrentStep = game.CurrentStep,
            Players = playersDto
        };
    }

    public async Task RevealTraitAsync(Guid gameId, Guid userId, string traitName)
    {
        var game = await _context.Games.Include(g => g.Players).FirstOrDefaultAsync(g => g.Id == gameId);
        if (game == null) throw new Exception("Game not found");
        if (!game.WorkflowInstanceId.HasValue) throw new Exception("Game workflow not started");

        // --- ВАЛИДАЦИЯ ОЧЕРЕДИ ХОДА (НОВОЕ) ---
        var wfInstance = await _persistenceProvider.GetWorkflowInstance(game.WorkflowInstanceId.Value.ToString());
        var data = wfInstance.Data as GameData;

        if (data == null) throw new Exception("Workflow data corrupted");

        // 1. Проверяем, что индекс в допустимых пределах
        if (data.TurnOrder == null || 
            data.CurrentPlayerTurnIndex < 0 || 
            data.CurrentPlayerTurnIndex >= data.TurnOrder.Count)
        {
            throw new InvalidOperationException("Game is in invalid state or voting phase.");
        }

        // 2. Проверяем, что сейчас ход именно этого игрока
        var activePlayerId = data.TurnOrder[data.CurrentPlayerTurnIndex];
        if (activePlayerId != userId)
        {
            throw new InvalidOperationException("It is not your turn to reveal a trait.");
        }
        // ----------------------------------------

        var player = game.Players.FirstOrDefault(p => p.UserId == userId);
        if (player == null) throw new Exception("Player not found");

        var allowedTraits = new[] { 
            nameof(Player.Profession), nameof(Player.Physiology), nameof(Player.Psychology), 
            nameof(Player.Gender), nameof(Player.Inventory), nameof(Player.Hobby), 
            nameof(Player.SpecialSkill), nameof(Player.CharacterTrait), nameof(Player.AdditionalInfo) 
        };

        if (!allowedTraits.Contains(traitName))
        {
            throw new InvalidOperationException($"Invalid trait name: {traitName}");
        }
        
        if (player.RevealedTraitKeys == null) player.RevealedTraitKeys = new List<string>();

        if (player.RevealedTraitKeys.Contains(traitName)) 
        {
             // Если карта уже открыта - ничего страшного, но ход мы не должны пропускать "впустую", 
             // если клиент случайно нажал. Но по логике Workflow, если мы не пошлем PublishEvent, ход не перейдет.
             // Поэтому лучше кинуть ошибку, чтобы фронт понял, что нужно открыть что-то другое.
             throw new InvalidOperationException("This trait is already revealed.");
        }

        player.RevealedTraitKeys.Add(traitName);
        _context.Entry(player).Property(p => p.RevealedTraitKeys).IsModified = true;
        await _context.SaveChangesAsync();

        // 1. SignalR
        var traitValue = GetTraitValue(player, traitName);
        await _hubContext.Clients.Group(gameId.ToString()).SendAsync("TraitRevealed", new 
        { 
            userId = userId, 
            traitName = traitName,
            value = traitValue 
        });

        // 2. Workflow Next Step
        await _workflowController.PublishEvent("RevealAction", game.WorkflowInstanceId.Value.ToString(), userId);
    }
    
    private string GetTraitValue(Player player, string traitName)
    {
        var prop = typeof(Player).GetProperty(traitName);
        return prop?.GetValue(player)?.ToString() ?? "???";
    }

    public async Task VoteAsync(Guid gameId, Guid userId, List<Guid> targetPlayerIds)
    {
        var game = await _context.Games.Include(g => g.Players).FirstOrDefaultAsync(g => g.Id == gameId);
        if (game == null) throw new Exception("Game not found");
        if (!game.WorkflowInstanceId.HasValue) throw new Exception("Game workflow not started");

        var workflowInstance = await _persistenceProvider.GetWorkflowInstance(game.WorkflowInstanceId.Value.ToString());
        var data = workflowInstance.Data as GameData;
        if (data == null) throw new Exception("Workflow data corrupted");

        // Валидация
        if (targetPlayerIds.Count != data.VotesRequiredPerPlayer)
            throw new InvalidOperationException($"You must cast exactly {data.VotesRequiredPerPlayer} votes.");

        if (targetPlayerIds.Contains(userId))
            throw new InvalidOperationException("You cannot vote for yourself.");

        if (targetPlayerIds.Distinct().Count() != targetPlayerIds.Count)
            throw new InvalidOperationException("Votes must be unique.");

        var playerIdsInGame = game.Players.Select(p => p.UserId).ToHashSet();
        if (targetPlayerIds.Any(id => !playerIdsInGame.Contains(id)))
            throw new InvalidOperationException("One of the target players is not in this game.");

        if (data.CurrentRoundVotes != null && data.CurrentRoundVotes.ContainsKey(userId))
            throw new InvalidOperationException("You have already voted in this round.");

        // SignalR уведомление
        await _hubContext.Clients.Group(gameId.ToString()).SendAsync("PlayerVoted", new { userId = userId });

        // ИСПРАВЛЕНИЕ: Отправляем VoteEvent
        var eventData = new BunkerGame.Workflows.Events.VoteEvent 
        { 
            UserId = userId, 
            TargetIds = targetPlayerIds 
        };

        await _workflowController.PublishEvent("PlayerVoted", 
            game.WorkflowInstanceId.Value.ToString(), 
            eventData);
    }
}