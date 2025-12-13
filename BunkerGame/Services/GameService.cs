using BunkerGame.Data;
using BunkerGame.DTOs.Game;
using BunkerGame.Models;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface; // Предполагаем, что библиотека подключена

namespace BunkerGame.Services;

public class GameService : IGameService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkflowController _workflowController;

    // --- Словари (оставляем без изменений) ---
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
        // 1. Загружаем комнату ВМЕСТЕ с пользователями
        var room = await _context.Rooms
            .Include(r => r.Players) 
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null) throw new Exception("Room not found");
        
        // 2. Создаем объект игры
        var game = new Game
        {
            Id = Guid.NewGuid(),
            CurrentStep = "Initialization",
            StartedAt = DateTime.UtcNow
        };
        
        // Сразу добавляем игру в контекст, чтобы получить валидный ID для связей
        _context.Games.Add(game);

        // 3. Генерируем персонажей и обновляем состояние Users
        var random = new Random();
        
        foreach (var user in room.Players)
        {
            // Генерация характеристик
            var age = random.Next(18, 90);
            var health = _physiologyConditions[random.Next(_physiologyConditions.Length)];
            
            var player = new Player
            {
                UserId = user.Id,
                GameId = game.Id, // Привязываем к игре
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
                VoteCount = 0,
                JoinedAt = DateTime.UtcNow
            };
            
            // Добавляем игрока в БД
            _context.Players.Add(player);

            // --- ОБНОВЛЕНИЕ СОСТОЯНИЯ ПОЛЬЗОВАТЕЛЯ ---
            user.CurrentRoomId = null; // Убираем из комнаты
            user.CurrentGame = game;   // Помещаем в игру
            user.CurrentPlayerCharacter = player; // Связываем с текущим персонажем
        }

        // 4. Запускаем Workflow
        // (Предполагается, что GameData - это класс из твоей workflow логики)
        var workflowId = await _workflowController.StartWorkflow("BunkerGameWorkflow", 1, new { GameId = game.Id });
        game.WorkflowInstanceId = Guid.Parse(workflowId);

        // 5. Удаляем комнату
        // EF Core сам обработает CurrentRoomId = null для пользователей, 
        // но мы это уже сделали явно выше для надежности.
        _context.Rooms.Remove(room);
        
        await _context.SaveChangesAsync();
        return game.Id;
    }

    public async Task<object> GetGameStateForUserAsync(Guid gameId, Guid userId)
    {
        // Подгружаем Игру -> Игроков -> Пользователей
        var game = await _context.Games
            .Include(g => g.Players)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null) return null!;

        // Формируем DTO с учетом "Тумана войны" (скрытых карт)
        var playersDto = game.Players.Select(p => {
            bool isMe = p.UserId == userId;
            // Проверка на null для RevealedTraitKeys (на случай старых данных)
            var revealed = p.RevealedTraitKeys ?? new List<string>();

            return new
            {
                Id = p.Id,
                UserId = p.UserId, // Полезно знать ID юзера
                Name = p.User?.Name ?? "Unknown", // Берем имя из User
                AvatarUrl = p.User?.AvatarUrl,
                IsMe = isMe,
                IsKicked = p.IsKicked,
                
                // Характеристики показываем только если это "Я" или характеристика открыта
                Profession = (isMe || revealed.Contains(nameof(Player.Profession))) ? p.Profession : "???",
                Physiology = (isMe || revealed.Contains(nameof(Player.Physiology))) ? p.Physiology : "???",
                Psychology = (isMe || revealed.Contains(nameof(Player.Psychology))) ? p.Psychology : "???",
                Gender = (isMe || revealed.Contains(nameof(Player.Gender))) ? p.Gender : "???",
                Inventory = (isMe || revealed.Contains(nameof(Player.Inventory))) ? p.Inventory : "???",
                Hobby = (isMe || revealed.Contains(nameof(Player.Hobby))) ? p.Hobby : "???",
                SpecialSkill = (isMe || revealed.Contains(nameof(Player.SpecialSkill))) ? p.SpecialSkill : "???",
                CharacterTrait = (isMe || revealed.Contains(nameof(Player.CharacterTrait))) ? p.CharacterTrait : "???",
                AdditionalInfo = (isMe || revealed.Contains(nameof(Player.AdditionalInfo))) ? p.AdditionalInfo : "???",
            };
        });

        return new
        {
            GameId = game.Id,
            CurrentStep = game.CurrentStep,
            Players = playersDto
        };
    }
    
    public async Task<List<GameDto>> GetAllGamesAsync()
    {
        // Используем проекцию (Select), чтобы SQL запрос выбрал только Count,
        // не загружая данные всех игроков в память.
        return await _context.Games
            .Select(g => new GameDto
            {
                Id = g.Id,
                CurrentStep = g.CurrentStep,
                PlayerCount = g.Players.Count,
                StartedAt = g.StartedAt
            })
            .OrderByDescending(g => g.StartedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteGameAsync(Guid gameId)
    {
        var game = await _context.Games.FindAsync(gameId);
    
        if (game == null) return false;
        
        _context.Games.Remove(game);
    
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task RevealTraitAsync(Guid gameId, Guid userId, string traitName)
    {
        var game = await _context.Games
            .Include(g => g.Players)
            .FirstOrDefaultAsync(g => g.Id == gameId);
            
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
            // Для Postgres массивов или JSON, EF ChangeTracker должен понять изменение, 
            // но иногда требуется явное указание, если это List<string> отображаемый через ValueConversion
            _context.Entry(player).Property(p => p.RevealedTraitKeys).IsModified = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task VoteAsync(Guid gameId, Guid userId, Guid targetPlayerId)
    {
        throw new NotImplementedException("Voting logic will be in Workflow");
    }
}