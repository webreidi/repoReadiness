using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RepoReadiness.Configuration;
using RepoReadiness.Services;

namespace RepoReadiness.Assessors;

public class CustomAgentsAssessor : IAssessor
{
    public string CategoryName => "CustomAgents";
    public int MaxScore => 10;

    public void Assess()
    {
        Console.WriteLine("[7/8] Assessing Custom Agents...");

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
            AssessmentConfig.Scores["CustomAgents"] += 2;
            AssessmentConfig.Findings["CustomAgents"].Strengths.Add($"Found {foundAgents.Count} custom agent(s)");

            // Check agent configurations
            foreach (var agent in foundAgents.Take(3))
            {
                try
                {
                    var content = File.ReadAllText(agent);
                    if (content.Contains("name:") || content.Contains("description:") || content.Contains("# "))
                    {
                        AssessmentConfig.Scores["CustomAgents"] += 2;
                        AssessmentConfig.Findings["CustomAgents"].Strengths.Add($"Well-configured agent: {Path.GetFileName(agent)}");
                    }
                }
                catch { }
            }

            // Copilot recognition test
            if (AssessmentConfig.CopilotAvailable)
            {
                string response = CopilotService.AskCopilot("What specialized agents are available in this repository? List their names and purposes.");
                if (!string.IsNullOrWhiteSpace(response) && !response.StartsWith("Error") && response.Length > 50)
                {
                    AssessmentConfig.Scores["CustomAgents"] += 4;
                    AssessmentConfig.Findings["CustomAgents"].Strengths.Add("Copilot recognizes custom agents");
                }
            }
        }
        else
        {
            AssessmentConfig.Findings["CustomAgents"].Weaknesses.Add("No custom agents found");
            AssessmentConfig.Findings["CustomAgents"].Recommendations.Add("Consider creating specialized agents for domain-specific tasks");
        }
    }
}