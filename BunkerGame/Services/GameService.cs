using BunkerGame.Data;
using BunkerGame.DTOs.Game;
using BunkerGame.Models;
using BunkerGame.Workflows;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using System.Text.Json;

namespace BunkerGame.Services;

public class GameService : IGameService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkflowController _workflowController;

    // --- Словари для генерации ---
    private readonly string[] _professions = { "Врач", "Инженер", "Солдат", "Учитель", "Повар", "Программист", "Плотник", "Юрист" };
    private readonly string[] _physiologyConditions = { "Идеально здоров", "Астма", "Онкология (1 стадия)", "Бесплодие", "Аллергия на пыль", "Толстый", "Атлет" };
    private readonly string[] _psychologies = { "Клаустрофобия", "Арахнофобия", "Депрессия", "Биполярное расстройство", "Психически здоров", "Паранойя" };
    private readonly string[] _hobbies = { "Футбол", "Садоводство", "Шахматы", "Стрельба", "Алкоголизм", "Вышивание", "Охота" };
    private readonly string[] _traits = { "Лидер", "Эгоист", "Паникер", "Добрый", "Лжец", "Конфликтный", "Харизматичный" };
    private readonly string[] _inventories = { "Аптечка", "Фонарик", "Пистолет (1 патрон)", "Карты", "Бутылка воды", "Нож", "Рация" };
    private readonly string[] _specialSkills = { "Взлом замков", "Первая помощь", "Стрельба навскидку", "Готовка из ничего", "Убеждение", "Ремонт техники" };
    private readonly string[] _additionalInfos = { "Родственник мэра", "Знает код от бункера", "Был в тюрьме", "Скрывает укус зомби", "Выиграл в лотерею", "Бесплоден" };

    public GameService(ApplicationDbContext context, IWorkflowController workflowController)
    {
        _context = context;
        _workflowController = workflowController;
    }

    public async Task<Guid> StartGameAsync(Guid roomId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null) throw new Exception("Room not found");
        // if (room.PlayerIds.Count < 2) throw new Exception("Not enough players"); // Раскомментируй для продакшена

        // 1. Создаем объект игры
        var game = new Game
        {
            RoomId = roomId,
            PlayerIds = new List<Guid>(room.PlayerIds),
            CurrentStep = "Initialization"
        };

        // 2. Генерируем персонажей для каждого игрока
        var random = new Random();
        foreach (var userId in room.PlayerIds)
        {
            var age = random.Next(18, 90);
            var health = _physiologyConditions[random.Next(_physiologyConditions.Length)];
            
            var player = new Player
            {
                UserId = userId,
                GameId = game.Id,
                
                // 9 Характеристик
                Gender = random.Next(0, 2) == 0 ? "Мужчина" : "Женщина",
                Physiology = $"{age} лет, {health}", // Объединяем Возраст и Здоровье в Физиологию
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
            
            // Сохраняем игрока в БД
            _context.Add(player);
            game.Players.Add(player);
        }

        // 3. Запускаем Workflow
        var workflowId = await _workflowController.StartWorkflow("BunkerGameWorkflow", 1, new GameData { GameId = game.Id });
        game.WorkflowInstanceId = Guid.Parse(workflowId);

        _context.Games.Add(game);
        
        // Удаляем комнату, так как игра началась
        _context.Rooms.Remove(room);
        
        await _context.SaveChangesAsync();
        return game.Id;
    }

    public async Task<object> GetGameStateForUserAsync(Guid gameId, Guid userId)
    {
        // Загружаем игру и игроков
        var game = await _context.Games
            .Include(g => g.Players)
            .ThenInclude(p => p.User) // Чтобы знать имена
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null) return null!;

        // Формируем ответ, скрывая закрытые карты (Fog of War)
        var playersDto = game.Players.Select(p => {
            // Если это карта самого пользователя - показываем всё
            bool isMe = p.UserId == userId;
            
            // Если карта открыта глобально
            var revealed = p.RevealedTraitKeys ?? new List<string>();

            return new
            {
                Id = p.Id,
                Name = p.User?.UserName ?? "Unknown",
                IsMe = isMe,
                
                // 9 Характеристик: проверяем доступ к каждой
                Profession = (isMe || revealed.Contains(nameof(Player.Profession))) ? p.Profession : "???",
                Physiology = (isMe || revealed.Contains(nameof(Player.Physiology))) ? p.Physiology : "???",
                Psychology = (isMe || revealed.Contains(nameof(Player.Psychology))) ? p.Psychology : "???",
                Gender = (isMe || revealed.Contains(nameof(Player.Gender))) ? p.Gender : "???",
                Inventory = (isMe || revealed.Contains(nameof(Player.Inventory))) ? p.Inventory : "???",
                Hobby = (isMe || revealed.Contains(nameof(Player.Hobby))) ? p.Hobby : "???",
                SpecialSkill = (isMe || revealed.Contains(nameof(Player.SpecialSkill))) ? p.SpecialSkill : "???",
                CharacterTrait = (isMe || revealed.Contains(nameof(Player.CharacterTrait))) ? p.CharacterTrait : "???",
                AdditionalInfo = (isMe || revealed.Contains(nameof(Player.AdditionalInfo))) ? p.AdditionalInfo : "???",
                
                IsKicked = p.IsKicked
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

        // Валидация имени свойства (чтобы нельзя было открыть несуществующее поле "Abrakadabra")
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
            
            // Явно сообщаем EF Core, что поле изменилось (для List<string> это критично)
            _context.Entry(player).Property(p => p.RevealedTraitKeys).IsModified = true;
            
            await _context.SaveChangesAsync();
        }
        
        // В будущем здесь будет Event в Workflow
    }

    public async Task VoteAsync(Guid gameId, Guid userId, Guid targetPlayerId)
    {
        throw new NotImplementedException("Voting logic will be in Workflow");
    }
}