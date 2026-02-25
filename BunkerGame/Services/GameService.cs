using System.Runtime.CompilerServices;
using BunkerGame.Data;
using BunkerGame.Models;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;

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
    
    public async Task<Guid> StartGameAsync(Guid roomId, Guid userId, int additionalRounds)
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
            Phase = "Init",
            RoomId = roomId,
            StartedAt = DateTime.UtcNow,
            AdditionalRounds = additionalRounds,
            AvailablePlaces =  room.Players.Count/2
        };
        _context.Games.Add(game);
        
        var random = new Random();
        
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
            
            user.CurrentGame = game;
            user.CurrentPlayerCharacter = player;
        }
        
        game.Phase = "Presentation";
        _context.SaveChanges();
        return game.Id;
    }

    private List<PlayerCharacteristic> GenerateCharacteristics(Random random)
    {
        var age = random.Next(18, 100);
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

    public async Task PresentationTrait(Guid userId, string traitCode)
    {
        var game = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.CurrentGame)
            .Include(u => u.Players)
            .FirstOrDefaultAsync();
    }
    
    
    
}