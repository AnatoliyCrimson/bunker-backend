using BunkerGame.Data;
using BunkerGame.DTOs.Game;
using BunkerGame.Models;
using BunkerGame.Workflows; 
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;

namespace BunkerGame.Services;

public class GameService : IGameService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkflowController _workflowController;
    private readonly IPersistenceProvider _persistenceProvider; // <-- ИЗМЕНЕНИЕ 1

    // --- Словари ---
    private readonly string[] _professions = { "Врач", "Инженер", "Солдат", "Учитель", "Повар", "Программист", "Плотник", "Юрист" };
    private readonly string[] _physiologyConditions = { "Идеально здоров", "Астма", "Онкология (1 стадия)", "Бесплодие", "Аллергия на пыль", "Толстый", "Атлет" };
    private readonly string[] _psychologies = { "Клаустрофобия", "Арахнофобия", "Депрессия", "Биполярное расстройство", "Психически здоров", "Паранойя" };
    private readonly string[] _hobbies = { "Футбол", "Садоводство", "Шахматы", "Стрельба", "Алкоголизм", "Вышивание", "Охота" };
    private readonly string[] _traits = { "Лидер", "Эгоист", "Паникер", "Добрый", "Лжец", "Конфликтный", "Харизматичный" };
    private readonly string[] _inventories = { "Аптечка", "Фонарик", "Пистолет (1 патрон)", "Карты", "Бутылка воды", "Нож", "Рация" };
    private readonly string[] _specialSkills = { "Взлом замков", "Первая помощь", "Стрельба навскидку", "Готовка из ничего", "Убеждение", "Ремонт техники" };
    private readonly string[] _additionalInfos = { "Родственник мэра", "Знает код от бункера", "Был в тюрьме", "Скрывает укус зомби", "Выиграл в лотерею", "Бесплоден" };

    // <-- ИЗМЕНЕНИЕ 2: Внедряем IPersistenceProvider
    public GameService(ApplicationDbContext context, IWorkflowController workflowController, IPersistenceProvider persistenceProvider)
    {
        _context = context;
        _workflowController = workflowController;
        _persistenceProvider = persistenceProvider;
    }

    public async Task<Guid> StartGameAsync(Guid roomId)
    {
        var room = await _context.Rooms
            .Include(r => r.Players) 
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null) throw new Exception("Room not found");
        
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Phase = "Initialization",
            StartedAt = DateTime.UtcNow,
            AdditionalRounds = 0 
        };
        
        _context.Games.Add(game);

        var random = new Random();
        foreach (var user in room.Players)
        {
            var age = random.Next(18, 90);
            var health = _physiologyConditions[random.Next(_physiologyConditions.Length)];
            
            var player = new Player
            {
                UserId = user.Id,
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
                TotalScore = 0 
            };
            
            _context.Players.Add(player);

            user.CurrentRoomId = null;
            user.CurrentGame = game;
            user.CurrentPlayerCharacter = player;
        }

        var workflowData = new GameWorkflowData
        {
            GameId = game.Id,
            AdditionalRoundsConfig = game.AdditionalRounds,
            PlayerIds = new List<Guid>(), 
            CurrentVotes = new Dictionary<Guid, Guid>()
        };

        var workflowId = await _workflowController.StartWorkflow("BunkerGameWorkflow", 1, workflowData);
        
        game.WorkflowInstanceId = Guid.Parse(workflowId);
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

        var myPlayer = game.Players.FirstOrDefault(p => p.UserId == userId);
        var myPlayerId = myPlayer?.Id;

        var playersDto = game.Players.Select(p => {
            bool isMe = p.UserId == userId;
            var revealed = p.RevealedTraitKeys ?? new List<string>();

            return new
            {
                Id = p.Id,
                UserId = p.UserId,
                Name = p.User?.Name ?? "Unknown",
                AvatarUrl = p.User?.AvatarUrl,
                IsMe = isMe,
                TotalScore = p.TotalScore,
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

        bool yourTurnNow = false;
        if (game.Phase == "Opening" && game.CurrentTurnPlayerId != null && myPlayerId != null)
        {
            yourTurnNow = game.CurrentTurnPlayerId == myPlayerId;
        }

        return new
        {
            GameId = game.Id,
            Phase = game.Phase,
            CurrentRound = game.CurrentRoundNumber,
            AdditionalRounds = game.AdditionalRounds,
            AvailablePlaces = game.AvailablePlaces,
            CurrentTurnPlayerId = game.CurrentTurnPlayerId,
            YourTurnNow = yourTurnNow,
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
        
        if (game.Phase != "Opening") 
            throw new InvalidOperationException("Сейчас нельзя открывать карты (не фаза обсуждения).");

        var player = game.Players.FirstOrDefault(p => p.UserId == userId);
        if (player == null) throw new Exception("Player not found");

        if (game.CurrentTurnPlayerId != player.Id)
        {
            throw new InvalidOperationException("Сейчас не ваш ход!");
        }

        var allowedTraits = new[] { 
            nameof(Player.Profession), nameof(Player.Physiology), nameof(Player.Psychology), 
            nameof(Player.Gender), nameof(Player.Inventory), nameof(Player.Hobby), 
            nameof(Player.SpecialSkill), nameof(Player.CharacterTrait), nameof(Player.AdditionalInfo) 
        };

        if (!allowedTraits.Contains(traitName))
            throw new InvalidOperationException($"Invalid trait name: {traitName}");
        
        if (player.RevealedTraitKeys == null) player.RevealedTraitKeys = new List<string>();

        if (!player.RevealedTraitKeys.Contains(traitName))
        {
            player.RevealedTraitKeys.Add(traitName);
            _context.Entry(player).Property(p => p.RevealedTraitKeys).IsModified = true;
            await _context.SaveChangesAsync();
        }

        await _workflowController.PublishEvent("TraitRevealed", gameId.ToString(), null);
    }

    public async Task VoteAsync(Guid gameId, Guid userId, Guid targetPlayerId)
    {
        var game = await _context.Games.Include(g => g.Players).FirstOrDefaultAsync(g => g.Id == gameId);
        if (game == null) throw new Exception("Game not found");

        if (game.Phase != "Voting")
            throw new InvalidOperationException("Голосование сейчас не идет.");

        var voter = game.Players.FirstOrDefault(p => p.UserId == userId);
        if (voter == null) throw new Exception("You are not in this game");
        
        if (voter.Id == targetPlayerId)
            throw new InvalidOperationException("Нельзя голосовать за себя.");

        if (game.WorkflowInstanceId == null) throw new Exception("Workflow not found");

        // --- ИСПРАВЛЕНИЕ ЗДЕСЬ ---
        // Добавили .ToString(), так как GetWorkflowInstance принимает string
        var wfInstance = await _persistenceProvider.GetWorkflowInstance(game.WorkflowInstanceId.Value.ToString());
        
        if (wfInstance == null) throw new Exception("Active workflow instance not found");

        var data = (GameWorkflowData)wfInstance.Data;

        if (data.CurrentVotes.ContainsKey(voter.Id))
        {
            throw new InvalidOperationException("Вы уже проголосовали в этом раунде.");
        }

        data.CurrentVotes[voter.Id] = targetPlayerId;

        // Сохраняем обновленные данные workflow
        await _persistenceProvider.PersistWorkflow(wfInstance);

        // Проверяем, все ли проголосовали
        if (data.CurrentVotes.Count >= game.Players.Count)
        {
            await _workflowController.PublishEvent("VotingFinished", gameId.ToString(), null);
        }
    }
}