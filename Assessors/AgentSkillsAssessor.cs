using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RepoReadiness.Configuration;
using RepoReadiness.Services;

namespace RepoReadiness.Assessors;

/// <summary>
/// Assesses agent skills - awards BONUS points (not part of base score).
/// Agent skills enhance Copilot but aren't required for it to work well.
/// </summary>
public class AgentSkillsAssessor : IAssessor
{
    public string CategoryName => "AgentSkills";
    public int MaxScore => 5; // Bonus points

    public void Assess()
    {
        Console.WriteLine("[10/10] Assessing Agent Skills (Bonus)...");

        var skillIndicators = new[] { "SKILL.md", "skill.yaml", "skill.yml", "*.skill.md" };
        var foundSkills = new List<string>();

        foreach (var pattern in skillIndicators)
        {
            try
            {
                var files = Directory.GetFiles(AssessmentConfig.RepoPath, pattern, SearchOption.AllDirectories)
                    .Where(f => !f.Contains("node_modules") && !f.Contains(".git"));
                foundSkills.AddRange(files);
            }
            catch { }
        }

        // Check .copilot/skills directory
        var skillsDir = Path.Combine(AssessmentConfig.RepoPath, ".copilot", "skills");
        if (Directory.Exists(skillsDir))
        {
            AssessmentConfig.BonusScores["AgentSkills"] += 2;
            AssessmentConfig.Findings["AgentSkills"].Strengths.Add("Skills directory found: .copilot/skills/ (+2 bonus)");

            try
            {
                var skillFiles = Directory.GetFiles(skillsDir, "*.*", SearchOption.AllDirectories);
                foundSkills.AddRange(skillFiles);
            }
            catch { }
        }

        foundSkills = foundSkills.Distinct().ToList();

        if (foundSkills.Any())
        {
            // +2 for skill definitions found (if not already awarded for directory)
            if (AssessmentConfig.BonusScores["AgentSkills"] == 0)
            {
                AssessmentConfig.BonusScores["AgentSkills"] += 2;
            }
            AssessmentConfig.Findings["AgentSkills"].Strengths.Add($"Found {foundSkills.Count} skill definition(s)");

            // +2 for documented skills (check first one)
            foreach (var skill in foundSkills.Take(1))
            {
                try
                {
                    var content = File.ReadAllText(skill);
                    if (content.Length > 100)
                    {
                        AssessmentConfig.BonusScores["AgentSkills"] += 2;
                        AssessmentConfig.Findings["AgentSkills"].Strengths.Add($"Documented skill: {Path.GetFileName(skill)} (+2 bonus)");
                    }
                }
                catch { }
            }

            // +1 for Copilot identification
            if (AssessmentConfig.CopilotAvailable)
            {
                string response = CopilotService.AskCopilot("What specialized skills are configured in this repository? Describe what each skill does.");
                if (!string.IsNullOrWhiteSpace(response) && !response.StartsWith("Error") && response.Length > 50)
                {
                    AssessmentConfig.BonusScores["AgentSkills"] += 1;
                    AssessmentConfig.Findings["AgentSkills"].Strengths.Add("Copilot identifies available skills (+1 bonus)");
                }
            }
        }
        else
        {
            AssessmentConfig.Findings["AgentSkills"].Recommendations.Add("Consider creating skills for reusable operations (optional, awards bonus points)");
        }
    }
}