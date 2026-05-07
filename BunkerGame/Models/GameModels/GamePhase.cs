using System.ComponentModel;

namespace BunkerGame.Models.GameModels;

public enum GamePhase
{
    // добавить предысторию
    [Description("Инициализация")]
    Init = 0,
    [Description("Предыстория и Бункер")]
    Story = 5,
    [Description("Открытие характеристик")]
    Turn = 1,
    [Description("Обсуждение")]
    Discussion = 2,
    [Description("Голосование")]
    Voting = 3,
    [Description("Игра завершена")]
    End = 4
}