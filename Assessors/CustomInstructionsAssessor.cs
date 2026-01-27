using System;
using System.IO;
using System.Linq;
using RepoReadiness.Configuration;
using RepoReadiness.Services;

namespace RepoReadiness.Assessors;

public class CustomInstructionsAssessor : IAssessor
{
    public string CategoryName => "CustomInstructions";
    public int MaxScore => 15;

    public void Assess()
    {
        Console.WriteLine("[6/8] Assessing Custom Instructions...");

        var instructionPaths = new[]
        {
            Path.Combine(AssessmentConfig.RepoPath, ".github", "copilot-instructions.md"),
            Path.Combine(AssessmentConfig.RepoPath, ".copilot-instructions.md"),
            Path.Combine(AssessmentConfig.RepoPath, "COPILOT.md")
        };

        string? foundPath = instructionPaths.FirstOrDefault(File.Exists);

        if (foundPath != null)
        {
            AssessmentConfig.Scores["CustomInstructions"] += 3;
            AssessmentConfig.Findings["CustomInstructions"].Strengths.Add($"Custom instructions file found: {Path.GetFileName(foundPath)}");

            var content = File.ReadAllText(foundPath);
            var lines = content.Split('\n').Length;

            // Content length scoring
            if (lines >= 50)
            {
                AssessmentConfig.Scores["CustomInstructions"] += 2;
                AssessmentConfig.Findings["CustomInstructions"].Strengths.Add("Comprehensive instructions content");
            }

            // Check for key sections
            var keySections = new[] { "coding standards", "naming", "testing", "security", "architecture" };
            int sectionsFound = 0;
            foreach (var section in keySections)
            {
                if (content.Contains(section, StringComparison.OrdinalIgnoreCase))
                    sectionsFound++;
            }

            AssessmentConfig.Scores["CustomInstructions"] += Math.Min(sectionsFound, 5);
            if (sectionsFound >= 3)
                AssessmentConfig.Findings["CustomInstructions"].Strengths.Add($"Covers {sectionsFound} key topics");

            // Copilot understanding test
            if (AssessmentConfig.CopilotAvailable)
            {
                string response = CopilotService.AskCopilot("Based on the custom instructions in this repository, what are the 3 most important coding standards to follow?");
                if (!string.IsNullOrWhiteSpace(response) && !response.StartsWith("Error"))
                {
                    int understanding = CopilotService.EvaluateCopilotUnderstanding(response, content);
                    AssessmentConfig.Scores["CustomInstructions"] += Math.Min(understanding, 5);
                    if (understanding >= 4)
                        AssessmentConfig.Findings["CustomInstructions"].Strengths.Add("Copilot correctly interprets custom instructions");
                }
            }
        }
        else
        {
            AssessmentConfig.Findings["CustomInstructions"].Weaknesses.Add("No custom instructions file found");
            AssessmentConfig.Findings["CustomInstructions"].Recommendations.Add("Create .github/copilot-instructions.md with project-specific guidance");
        }
    }
}