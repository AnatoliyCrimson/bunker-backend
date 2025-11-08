// Models/GameData.cs
using System.Collections.Generic;
using BunkerGame.Workflows;

namespace BunkerGame.Models
{
    public class GameData
    {
        public GameWorkflowContext Context { get; set; } = new();
    }
}