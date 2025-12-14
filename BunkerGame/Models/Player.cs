namespace BunkerGame.Models;

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public Guid GameId { get; set; }
    public Game? Game { get; set; }
    
    // ... Характеристики (Physiology, Psychology и т.д.) оставляем без изменений ...
    public string Physiology { get; set; } = string.Empty;
    public string Psychology { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Profession { get; set; } = string.Empty;
    public string Inventory { get; set; } = string.Empty;
    public string Hobby { get; set; } = string.Empty;
    public string SpecialSkill { get; set; } = string.Empty;
    public string CharacterTrait { get; set; } = string.Empty;
    public string AdditionalInfo { get; set; } = string.Empty;
    
    public List<string> RevealedTraitKeys { get; set; } = new(); // список открытых характеристик
    public int TotalScore { get; set; } = 0; // количество баллов у игрока
}