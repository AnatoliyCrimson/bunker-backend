namespace BunkerGame.Models;

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public Guid GameId { get; set; }
    public Game? Game { get; set; }
    
    public List<PlayerCharacteristic> Characteristics { get; set; } = new();
    
    public List<string> RevealedTraitKeys { get; set; } = new(); // список открытых характеристик
    public int TotalScore { get; set; } = 0; // количество баллов у игрока
}