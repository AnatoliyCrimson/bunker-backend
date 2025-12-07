using BunkerGame.Data;
using BunkerGame.DTOs.Game;
using BunkerGame.Hubs; // Добавлено для SignalR
using BunkerGame.Models;
using BunkerGame.Workflows;
using Microsoft.AspNetCore.SignalR; // Добавлено для SignalR
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;

namespace BunkerGame.Services;

public class GameService : IGameService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkflowController _workflowController;
    private readonly IPersistenceProvider _persistenceProvider;
    private readonly IHubContext<GameHub> _hubContext; // <-- НОВОЕ: Для отправки уведомлений

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
        IHubContext<GameHub> hubContext) // <-- Инжектим HubContext
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
            CurrentStep = "Initialization"
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

        var workflowId = await _workflowController.StartWorkflow("BunkerGameWorkflow", 1, new GameData { GameId = game.Id });
        game.WorkflowInstanceId = Guid.Parse(workflowId);

        _context.Games.Add(game);
        _context.Rooms.Remove(room);
        
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

        var playersDto = game.Players.Select(p => {
            bool isMe = p.UserId == userId;
            var revealed = p.RevealedTraitKeys ?? new List<string>();
            bool showAll = revealed.Contains("ALL"); 

            return new
            {
                Id = p.Id,
                UserId = p.UserId, // Полезно знать ID пользователя
                Name = p.User?.UserName ?? "Unknown",
                AvatarUrl = p.User?.AvatarUrl, // <-- НОВОЕ: Возвращаем аватарку
                IsMe = isMe,
                
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
                RevealedTraits = revealed // Фронту полезно знать, что именно открыто (список ключей)
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

        // Если уже открыто, просто выходим (или кидаем ошибку)
        if (player.RevealedTraitKeys.Contains(traitName)) return;

        player.RevealedTraitKeys.Add(traitName);
        _context.Entry(player).Property(p => p.RevealedTraitKeys).IsModified = true;
        await _context.SaveChangesAsync();

        // 1. Уведомляем всех через SignalR, чтобы обновили интерфейс
        // Передаем ID игрока, имя характеристики и (важно!) само значение, 
        // так как оно теперь открыто для всех.
        var traitValue = GetTraitValue(player, traitName);
        
        await _hubContext.Clients.Group(gameId.ToString()).SendAsync("TraitRevealed", new 
        { 
            userId = userId, 
            traitName = traitName,
            value = traitValue 
        });

        // 2. Двигаем Workflow
        if (game.WorkflowInstanceId.HasValue)
        {
            await _workflowController.PublishEvent("RevealAction", game.WorkflowInstanceId.Value.ToString(), userId);
        }
    }
    
    // Вспомогательный метод для получения значения свойства через Reflection
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

        // Уведомление (Анонимное), что кто-то проголосовал (для прогресс-бара)
        await _hubContext.Clients.Group(gameId.ToString()).SendAsync("PlayerVoted", new { userId = userId });

        await _workflowController.PublishEvent("PlayerVoted", 
            game.WorkflowInstanceId.Value.ToString(), 
            new Tuple<Guid, List<Guid>>(userId, targetPlayerIds));
    }
}