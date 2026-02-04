using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RepoReadiness.Configuration;
using RepoReadiness.Services;

namespace RepoReadiness.Assessors;

/// <summary>
/// Assesses custom agents - awards BONUS points (not part of base score).
/// Custom agents enhance Copilot but aren't required for it to work well.
/// </summary>
public class CustomAgentsAssessor : IAssessor
{
    public string CategoryName => "CustomAgents";
    public int MaxScore => 5; // Bonus points

    public void Assess()
    {
        Console.WriteLine("[10/11] Assessing Custom Agents (Bonus)...");

        var agentPatterns = new[] { "*.agent.md", "*.agent.yaml", "*.agent.yml" };
        var foundAgents = new List<string>();

        foreach (var pattern in agentPatterns)
        {
            try
            {
                var files = Directory.GetFiles(AssessmentConfig.RepoPath, pattern, SearchOption.AllDirectories)
                    .Where(f => !f.Contains("node_modules") && !f.Contains(".git"));
                foundAgents.AddRange(files);
            }
            catch { }
        }

        // Also check .github/agents directory
        var agentsDir = Path.Combine(AssessmentConfig.RepoPath, ".github", "agents");
        if (Directory.Exists(agentsDir))
        {
            try
            {
                var agentFiles = Directory.GetFiles(agentsDir, "*.*", SearchOption.AllDirectories);
                foundAgents.AddRange(agentFiles);
            }
            catch { }
        }

        foundAgents = foundAgents.Distinct().ToList();

        if (foundAgents.Any())
        {
            // +2 for having agent files
            AssessmentConfig.BonusScores["CustomAgents"] += 2;
            AssessmentConfig.Findings["CustomAgents"].Strengths.Add($"Found {foundAgents.Count} custom agent(s) (+2 bonus)");

            // +2 for well-configured agents (check first one only for simplicity)
            foreach (var agent in foundAgents.Take(1))
            {
                try
                {
                    var content = File.ReadAllText(agent);
                    if (content.Contains("name:") || content.Contains("description:") || content.Contains("# "))
                    {
                        AssessmentConfig.BonusScores["CustomAgents"] += 2;
                        AssessmentConfig.Findings["CustomAgents"].Strengths.Add($"Well-configured agent: {Path.GetFileName(agent)} (+2 bonus)");
                    }
                }
                catch { }
            }

            // +1 for Copilot recognition
            if (AssessmentConfig.CopilotAvailable)
            {
                string response = CopilotService.AskCopilot("What specialized agents are available in this repository? List their names and purposes.");
                if (!string.IsNullOrWhiteSpace(response) && !response.StartsWith("Error") && response.Length > 50)
                {
                    AssessmentConfig.BonusScores["CustomAgents"] += 1;
                    AssessmentConfig.Findings["CustomAgents"].Strengths.Add("Copilot recognizes custom agents (+1 bonus)");
                }
            }
        }
        else
        {
            AssessmentConfig.Findings["CustomAgents"].Recommendations.Add("Consider creating specialized agents for domain-specific tasks (optional, awards bonus points)");
        }
    }
}