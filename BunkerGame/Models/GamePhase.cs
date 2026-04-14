using System.ComponentModel;

namespace BunkerGame.Models;

public enum GamePhase
{
    // добавить предысторию
    [Description("Инициализация")]
    Init = 0,
    [Description("Открытие характеристик")]
    Turn = 1,
    [Description("Обсуждение")]
    Discussion = 2,
    [Description("Голосование")]
    Voting = 3,
    [Description("Игра завершена")]
    End = 4
}