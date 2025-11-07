namespace BunkerGame.Models;

public class PlayerProfile
{
    public string Profession { get; set; } = string.Empty;       // Профессия
    public string Health { get; set; } = string.Empty;           // Здоровье
    public string Age { get; set; } = string.Empty;              // Возраст
    public string Gender { get; set; } = string.Empty;           // Пол
    public string CharacterTrait { get; set; } = string.Empty;   // Черта характера
    public string Hobby { get; set; } = string.Empty;            // Хобби
    public string Phobia { get; set; } = string.Empty;           // Фобия
    public string AdditionalInfo { get; set; } = string.Empty;   // Дополнительная информация
    public string Fact1 { get; set; } = string.Empty;            // Факт №1 (иногда используется)
    public string Fact2 { get; set; } = string.Empty;            // Факт №2
}