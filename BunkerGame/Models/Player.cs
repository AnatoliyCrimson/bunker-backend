using System.ComponentModel.DataAnnotations.Schema;

namespace BunkerGame.Models;

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Связи
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public Guid GameId { get; set; }
    public Game? Game { get; set; }
    
    // --- 9 Характеристик (The 9 Traits) ---
    public string Physiology { get; set; } = string.Empty;       // Физиология
    public string Psychology { get; set; } = string.Empty;       // Психология
    public string Gender { get; set; } = string.Empty;           // Пол
    public string Profession { get; set; } = string.Empty;       // Профессия
    public string Inventory { get; set; } = string.Empty;        // Инвентарь
    public string Hobby { get; set; } = string.Empty;            // Хобби
    public string SpecialSkill { get; set; } = string.Empty;     // Особые умения
    public string CharacterTrait { get; set; } = string.Empty;   // Черта характера
    public string AdditionalInfo { get; set; } = string.Empty;   // Доп. сведения
    
    // --- Состояние (State) ---
    
    // Список названий свойств, которые открыты для всех (например: "Profession", "Health")
    // В БД это можно хранить как JSON или массив строк (Postgres поддерживает)
    public List<string> RevealedTraitKeys { get; set; } = new();

    public int VoteCount { get; set; } = 0; // Полученные голоса в текущем раунде
    public bool IsKicked { get; set; } = false; // Выбыл или прошел в бункер (зависит от логики финала)
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}