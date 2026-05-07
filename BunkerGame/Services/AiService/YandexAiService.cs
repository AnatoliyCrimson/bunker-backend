using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using BunkerGame.Data;
using BunkerGame.Models;
using BunkerGame.DTOs.Ai;
using BunkerGame.Models.GameModels;
using Microsoft.EntityFrameworkCore;

namespace BunkerGame.Services.AiService;

public class YandexAiOptions
{
    public string ApiKey { get; set; } = null!;
    public string FolderId { get; set; } = null!;
    public string ModelName { get; set; } = null!;
}

public class YandexAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly YandexAiOptions _options;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<YandexAiService> _logger;

    public YandexAiService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ApplicationDbContext context,
        ILogger<YandexAiService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
        
        _options = configuration.GetSection("AI:Yandex").Get<YandexAiOptions>() 
                   ?? throw new Exception("AI:Yandex configuration is missing");

        _httpClient.BaseAddress = new Uri("https://llm.api.cloud.yandex.net/foundationModels/v1/completion");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Api-Key {_options.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("x-folder-id", _options.FolderId);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
    
    public async Task<AiStoryResponse?> GenerateInitialStoryAsync(int playerCount, int availablePlaces)
    {
        var prompt = await GetPromptAsync("game_init_story");
        if (prompt == null)
        {
            _logger.LogError("Prompt 'game_init_story' not found in database");
            return null;
        }

        string userText = prompt.UserPromptTemplate
            .Replace("{playerCount}", playerCount.ToString())
            .Replace("{availablePlaces}", availablePlaces.ToString());
        
        var requestBody = CreateRequest(prompt.SystemPrompt, userText, prompt.Temperature, GetStorySchema());

        try 
        {
            var response = await _httpClient.PostAsJsonAsync("", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            string jsonText = result.GetProperty("result").GetProperty("alternatives")[0].GetProperty("message").GetProperty("text").GetString()!;

            jsonText = CleanJsonResponse(jsonText);

            return JsonSerializer.Deserialize<AiStoryResponse>(jsonText, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Yandex AI for story generation");
            return null;
        }
    }

    public async Task<AiVerdictResponse?> GenerateFinalVerdictAsync(Game game, List<GameEventLog> logs)
    {
        var prompt = await GetPromptAsync("game_final_verdict");
        if (prompt == null)
        {
            _logger.LogError("Prompt 'game_final_verdict' not found in database");
            return null;
        }

        // 1. Формируем описание начальных условий
        string initialConditions = $@"Катастрофа: {game.DisasterName}
            Описание катастрофы: {game.DisasterDescription}
            Описание бункера: {game.BunkerDescription}
            Комнаты в бункере:
            {string.Join("\n", game.BunkerRooms.Select(r => $"- {r.Name} ({r.Status}): {r.Description}"))}";

        // 2. Формируем список выживших и изгнанных
        // В игре "Бункер" выживают те, за кого больше всего голосовали (AvailablePlaces человек)
        var survivors = game.Players
            .OrderByDescending(p => p.TotalScore)
            .Take(game.AvailablePlaces)
            .Select(p => p.Id)
            .ToHashSet();

        string playersInfo = string.Join("\n", game.Players.Select(p => 
        {
            var status = survivors.Contains(p.Id) ? "[ВЫЖИЛ]" : "[ИЗГНАН]";
            var traits = string.Join(", ", p.Characteristics.Select(c => $"{c.Label}: {c.Value}"));
            return $"- {status} {p.User?.Name ?? "Неизвестный"}. Характеристики: {traits}";
        }));

        // 3. Формируем описание логов
        string logsDescription = string.Join("\n", logs.Select(l => $"- [{l.Timestamp:T}] {l.Description}"));
        
        // 4. Собираем финальный текст промпта
        string userText = prompt.UserPromptTemplate
            .Replace("{initial_conditions}", initialConditions)
            .Replace("{players_info}", playersInfo)
            .Replace("{logs}", logsDescription);
        
        var requestBody = CreateRequest(prompt.SystemPrompt, userText, prompt.Temperature, GetVerdictSchema());

        try 
        {
            var response = await _httpClient.PostAsJsonAsync("", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            string jsonText = result.GetProperty("result").GetProperty("alternatives")[0].GetProperty("message").GetProperty("text").GetString()!;

            jsonText = CleanJsonResponse(jsonText);

            return JsonSerializer.Deserialize<AiVerdictResponse>(jsonText, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Yandex AI for final verdict");
            return null;
        }
    }

    private async Task<AiPrompt?> GetPromptAsync(string code)
    {
        return await _context.AiPrompts.FirstOrDefaultAsync(p => p.Code == code);
    }

    private string CleanJsonResponse(string jsonText)
    {
        if (string.IsNullOrWhiteSpace(jsonText)) return jsonText;

        jsonText = jsonText.Trim();

        // Remove Markdown code blocks if present (```json or ```)
        if (jsonText.StartsWith("```"))
        {
            // Find the index of the first newline after the opening backticks
            int firstNewLine = jsonText.IndexOf('\n');
            if (firstNewLine != -1)
            {
                jsonText = jsonText.Substring(firstNewLine).Trim();
            }
            else
            {
                // Fallback for cases like ```{...}```
                jsonText = jsonText.Replace("```json", "").Replace("```", "").Trim();
            }

            // Remove closing backticks
            if (jsonText.EndsWith("```"))
            {
                jsonText = jsonText.Substring(0, jsonText.Length - 3).Trim();
            }
        }

        return jsonText.Trim();
    }

    private object CreateRequest(string system, string user, float temperature, object schema)
    {
        string modelName = string.IsNullOrEmpty(_options.ModelName) ? "yandexgpt" : _options.ModelName;
        return new
        {
            modelUri = $"gpt://{_options.FolderId}/{modelName}",
            completionOptions = new
            {
                stream = false,
                temperature = temperature,
                maxTokens = "2000"
            },
            messages = new[]
            {
                new { role = "system", text = system},
                new { role = "user", text = user }
            },
            // Response format with JSON Schema (latest Yandex GPT API feature)
            responseFormat = new
            {
                type = "json_schema", 
                json_schema = new
                {
                    schema = schema
                }
            }
        };
    }

    private object GetStorySchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                disasterName = new { type = "string", description = "Название катастрофы" },
                disasterDescription = new { type = "string", description = "Описание катастрофы" },
                bunkerDescription = new { type = "string", description = "Описание бункера" },
                rooms = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string" },
                            status = new { type = "string" },
                            description = new { type = "string" }
                        },
                        required = new[] { "name", "status", "description" }
                    }
                },
                players = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            bio = new { type = "string" },
                            health = new { type = "string" },
                            profession = new { type = "string" },
                            character = new { type = "string" },
                            hobby = new { type = "string" },
                            phobia = new { type = "string" },
                            inventory = new { type = "string" },
                            knowledge = new { type = "string" },
                            info = new { type = "string" }
                        },
                        required = new[] { "bio", "health", "profession", "character", "hobby", "phobia", "inventory", "knowledge", "info" }
                    }
                }
            },
            required = new[] { "disasterName", "disasterDescription", "bunkerDescription", "rooms", "players" }
        };
    }

    private object GetVerdictSchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                score = new { type = "integer", description = "Интегральный показатель успеха (0-100)" },
                impactAnalysis = new { type = "string", description = "Анализ влияния выбора игроков" },
                survivalTimeline = new
                {
                    type = "array",
                    description = "Прогноз событий на 1, 5, 10 лет",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            period = new { type = "string", description = "Период времени (например, '1 год')" },
                            @event = new { type = "string", description = "Описание события" }
                        },
                        required = new[] { "period", "event" }
                    }
                },
                bestAlternative = new 
                { 
                    type = "object", 
                    description = "Наилучшая альтернатива (что могло бы привести к лучшему результату)",
                    properties = new 
                    {
                        description = new { type = "string", description = "Описание сценария" },
                        score = new { type = "integer", description = "Интегральный показатель успеха для этого сценария" }
                    },
                    required = new[] { "description", "score" }
                },
                worstAlternative = new 
                { 
                    type = "object", 
                    description = "Наихудшая альтернатива (самый плохой вариант развития событий)",
                    properties = new 
                    {
                        description = new { type = "string", description = "Описание сценария" },
                        score = new { type = "integer", description = "Интегральный показатель успеха для этого сценария" }
                    },
                    required = new[] { "description", "score" }
                }
            },
            required = new[] { "score", "impactAnalysis", "survivalTimeline", "bestAlternative", "worstAlternative" }
        };
    }
}
