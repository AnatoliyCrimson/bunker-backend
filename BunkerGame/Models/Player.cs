using BunkerGame.Models.GameModels;

namespace BunkerGame.Models;

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public Guid GameId { get; set; }
    public Game? Game { get; set; }
    
    public bool IsVoted { get; set; } = false;
    public int TotalScore { get; set; } = 0; // количество баллов у игрока
    
    
    public List<PlayerCharacteristic> Characteristics { get; set; } = new();
    
    public List<string> PresentationTraitKeys { get; set; } = new(); // список открытых характеристик
}