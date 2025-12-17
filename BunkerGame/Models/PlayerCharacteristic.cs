namespace BunkerGame.Models;

public class PlayerCharacteristic
{
    public string Code { get; set; } = string.Empty;  // Например: "profession"
    public string Label { get; set; } = string.Empty; // Например: "Профессия"
    public string Value { get; set; } = string.Empty; // Например: "Врач"
    public bool IsOpen { get; set; } = false;         // Открыта ли характеристика
}