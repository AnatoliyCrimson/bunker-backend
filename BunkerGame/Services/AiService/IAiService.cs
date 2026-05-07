using BunkerGame.DTOs.Ai;
using BunkerGame.Models;
using BunkerGame.Models.GameModels;

namespace BunkerGame.Services.AiService;

public interface IAiService
{
    /// <summary>
    /// Генерирует начальную предысторию мира и описание бункера
    /// </summary>
    Task<AiStoryResponse?> GenerateInitialStoryAsync(int playerCount, int availablePlaces);

    /// <summary>
    /// Генерирует финальный вердикт на основе логов игры и характеристик
    /// </summary>
    Task<AiVerdictResponse?> GenerateFinalVerdictAsync(Game game, List<GameEventLog> logs);
}