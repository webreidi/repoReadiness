using System.Collections.Generic;
using System.Linq;
using RepoReadiness.Models;

namespace RepoReadiness.Configuration;

public static class AssessmentConfig
{
    public static bool VerboseMode { get; set; } = false;
    public static bool CopilotAvailable { get; set; } = false;
    public static string RepoPath { get; set; } = "";

    // Base scores (150 points max)
    public static Dictionary<string, int> Scores { get; } = new()
    {
        { "Build", 0 },
        { "Run", 0 },
        { "Test", 0 },
        { "CodeQuality", 0 },
        { "Documentation", 0 },
        { "CustomInstructions", 0 },
        { "TypeSafety", 0 },
        { "ContextFriendliness", 0 }
    };

    // Bonus scores (10 points max - not required for good grade)
    public static Dictionary<string, int> BonusScores { get; } = new()
    {
        { "CustomAgents", 0 },
        { "AgentSkills", 0 }
    };

    public static Dictionary<string, CategoryFindings> Findings { get; } = new()
    {
        { "Build", new CategoryFindings() },
        { "Run", new CategoryFindings() },
        { "Test", new CategoryFindings() },
        { "CodeQuality", new CategoryFindings() },
        { "Documentation", new CategoryFindings() },
        { "CustomInstructions", new CategoryFindings() },
        { "TypeSafety", new CategoryFindings() },
        { "ContextFriendliness", new CategoryFindings() },
        { "CustomAgents", new CategoryFindings() },
        { "AgentSkills", new CategoryFindings() }
    };

    public static void Reset()
    {
        foreach (var key in Scores.Keys.ToList())
            Scores[key] = 0;
        foreach (var key in BonusScores.Keys.ToList())
            BonusScores[key] = 0;
        foreach (var key in Findings.Keys.ToList())
            Findings[key] = new CategoryFindings();
    }
}