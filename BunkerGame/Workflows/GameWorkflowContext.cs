// Models/GameWorkflowContext.cs
using BunkerGame.Models;
using System.Collections.Generic;

namespace BunkerGame.Models
{
    public class GameWorkflowContext
    {
        public int GameId { get; set; }
        public List<PlayerProfile> Players { get; set; } = new();
        public int CurrentRound { get; set; } = 1;
        public int MaxRounds { get; set; } = 10;
        public List<int> EliminatedPlayerIds { get; set; } = new();
        public List<int> BunkerPlayers { get; set; } = new();
        public Dictionary<int, int> Votes { get; set; } = new();
        public bool IsGameOver { get; set; }
        public string CurrentPhase { get; set; } = "Initialization";
        public Dictionary<string, object> TempData { get; set; } = new();
    }
}