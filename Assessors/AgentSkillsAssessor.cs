using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RepoReadiness.Configuration;
using RepoReadiness.Services;

namespace RepoReadiness.Assessors;

public class AgentSkillsAssessor : IAssessor
{
    public string CategoryName => "AgentSkills";
    public int MaxScore => 10;

    public void Assess()
    {
        Console.WriteLine("[8/8] Assessing Agent Skills...");

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
            AssessmentConfig.Scores["AgentSkills"] += 2;
            AssessmentConfig.Findings["AgentSkills"].Strengths.Add("Skills directory found: .copilot/skills/");

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
            AssessmentConfig.Findings["AgentSkills"].Strengths.Add($"Found {foundSkills.Count} skill definition(s)");

            foreach (var skill in foundSkills.Take(3))
            {
                try
                {
                    var content = File.ReadAllText(skill);
                    if (content.Length > 100)
                    {
                        AssessmentConfig.Scores["AgentSkills"] += 2;
                        AssessmentConfig.Findings["AgentSkills"].Strengths.Add($"Documented skill: {Path.GetFileName(skill)}");
                    }
                }
                catch { }
            }

            // Copilot identification test
            if (AssessmentConfig.CopilotAvailable)
            {
                string response = CopilotService.AskCopilot("What specialized skills are configured in this repository? Describe what each skill does.");
                if (!string.IsNullOrWhiteSpace(response) && !response.StartsWith("Error") && response.Length > 50)
                {
                    AssessmentConfig.Scores["AgentSkills"] += 4;
                    AssessmentConfig.Findings["AgentSkills"].Strengths.Add("Copilot identifies available skills");
                }
            }
        }
        else
        {
            AssessmentConfig.Findings["AgentSkills"].Weaknesses.Add("No agent skills found");
            AssessmentConfig.Findings["AgentSkills"].Recommendations.Add("Consider creating skills for reusable operations (API calls, code generation templates)");
        }
    }
}