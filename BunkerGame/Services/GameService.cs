using BunkerGame.Data;
using BunkerGame.Models;
using BunkerGame.Workflows;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;

namespace BunkerGame.Services;

public class GameService : IGameService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkflowController _workflowController;
    private readonly IPersistenceProvider _persistenceProvider; // Для чтения состояния Workflow

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
        IPersistenceProvider persistenceProvider)
    {
        _context = context;
        _workflowController = workflowController;
        _persistenceProvider = persistenceProvider;
    }

    public async Task<Guid> StartGameAsync(Guid roomId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) throw new Exception("Room not found");
        
        // Создаем объект игры
        var game = new Game
        {
            RoomId = roomId,
            PlayerIds = new List<Guid>(room.PlayerIds),
            CurrentStep = "Initialization"
        };

        // Генерируем персонажей
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

        // Запускаем Workflow
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

            // Если игра окончена (все карты открыты), или карта своя, или открыта явно
            bool showAll = revealed.Contains("ALL"); 

            return new
            {
                Id = p.Id,
                Name = p.User?.UserName ?? "Unknown",
                IsMe = isMe,
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
                VoteCount = p.VoteCount // Можно показывать текущие баллы
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

        if (!player.RevealedTraitKeys.Contains(traitName))
        {
            player.RevealedTraitKeys.Add(traitName);
            _context.Entry(player).Property(p => p.RevealedTraitKeys).IsModified = true;
            await _context.SaveChangesAsync();
        }

        // Продвигаем Workflow (событие открытия карты)
        // Workflow ждет события "RevealAction", чтобы передать ход следующему
        if (game.WorkflowInstanceId.HasValue)
        {
            await _workflowController.PublishEvent("RevealAction", game.WorkflowInstanceId.Value.ToString(), userId);
        }
    }

    public async Task VoteAsync(Guid gameId, Guid userId, List<Guid> targetPlayerIds)
    {
        var game = await _context.Games.Include(g => g.Players).FirstOrDefaultAsync(g => g.Id == gameId);
        if (game == null) throw new Exception("Game not found");

        if (!game.WorkflowInstanceId.HasValue) throw new Exception("Game workflow not started");

        // 1. Читаем состояние Workflow, чтобы узнать правила текущего этапа (VotesRequiredPerPlayer)
        var workflowInstance = await _persistenceProvider.GetWorkflowInstance(game.WorkflowInstanceId.Value.ToString());
        var data = workflowInstance.Data as GameData;

        if (data == null) throw new Exception("Workflow data corrupted");

        // 2. ВАЛИДАЦИЯ

        // 2.1. Количество голосов
        if (targetPlayerIds.Count != data.VotesRequiredPerPlayer)
        {
            throw new InvalidOperationException($"You must cast exactly {data.VotesRequiredPerPlayer} votes.");
        }

        // 2.2. Нельзя за себя
        if (targetPlayerIds.Contains(userId))
        {
            throw new InvalidOperationException("You cannot vote for yourself.");
        }

        // 2.3. Уникальность голосов
        if (targetPlayerIds.Distinct().Count() != targetPlayerIds.Count)
        {
            throw new InvalidOperationException("Votes must be unique (you cannot vote for the same person twice).");
        }

        // 2.4. Цели должны быть в игре
        var playerIdsInGame = game.Players.Select(p => p.UserId).ToHashSet();
        if (targetPlayerIds.Any(id => !playerIdsInGame.Contains(id)))
        {
            throw new InvalidOperationException("One of the target players is not in this game.");
        }

        // 2.5. Проверка: голосовал ли уже в этом раунде?
        if (data.CurrentRoundVotes != null && data.CurrentRoundVotes.ContainsKey(userId))
        {
            throw new InvalidOperationException("You have already voted in this round.");
        }

        // 3. Отправляем голос в Workflow
        // Передаем Tuple: (Кто, КогоСписок)
        await _workflowController.PublishEvent("PlayerVoted", 
            game.WorkflowInstanceId.Value.ToString(), 
            new Tuple<Guid, List<Guid>>(userId, targetPlayerIds));
    }
    
    // Перегрузка для IGameService (если интерфейс требует старую сигнатуру, то удали её в интерфейсе)
    // Но в твоем случае в IGameService нужно обновить сигнатуру VoteAsync.
    public async Task VoteAsync(Guid gameId, Guid userId, Guid targetPlayerId)
    {
        // Этот метод устарел, используй список
        await VoteAsync(gameId, userId, new List<Guid> { targetPlayerId });
    }
}