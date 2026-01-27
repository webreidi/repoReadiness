using System.Collections.Generic;
using System.Linq;
using RepoReadiness.Models;

namespace RepoReadiness.Configuration;

public static class AssessmentConfig
{
    public static bool VerboseMode { get; set; } = false;
    public static bool CopilotAvailable { get; set; } = false;
    public static string RepoPath { get; set; } = "";

    public static Dictionary<string, int> Scores { get; } = new()
    {
        { "Build", 0 },
        { "Run", 0 },
        { "Test", 0 },
        { "CodeUnderstanding", 0 },
        { "Documentation", 0 },
        { "CustomInstructions", 0 },
        { "CustomAgents", 0 },
        { "AgentSkills", 0 }
    };

    public static Dictionary<string, CategoryFindings> Findings { get; } = new()
    {
        { "Build", new CategoryFindings() },
        { "Run", new CategoryFindings() },
        { "Test", new CategoryFindings() },
        { "CodeUnderstanding", new CategoryFindings() },
        { "Documentation", new CategoryFindings() },
        { "CustomInstructions", new CategoryFindings() },
        { "CustomAgents", new CategoryFindings() },
        { "AgentSkills", new CategoryFindings() }
    };

    public static void Reset()
    {
        foreach (var key in Scores.Keys.ToList())
            Scores[key] = 0;
        foreach (var key in Findings.Keys.ToList())
            Findings[key] = new CategoryFindings();
    }
}