using System;
using System.IO;
using System.Linq;
using RepoReadiness.Configuration;
using RepoReadiness.Services;

namespace RepoReadiness.Assessors;

/// <summary>
/// Assesses custom instructions - these directly tell Copilot how to behave in this repo.
/// </summary>
public class CustomInstructionsAssessor : IAssessor
{
    public string CategoryName => "CustomInstructions";
    public int MaxScore => 20;

    public void Assess()
    {
        Console.WriteLine("[7/11] Assessing Custom Instructions...");

        var instructionPaths = new[]
        {
            Path.Combine(AssessmentConfig.RepoPath, ".github", "copilot-instructions.md"),
            Path.Combine(AssessmentConfig.RepoPath, ".copilot-instructions.md"),
            Path.Combine(AssessmentConfig.RepoPath, "COPILOT.md")
        };

        string? foundPath = instructionPaths.FirstOrDefault(File.Exists);

        if (foundPath != null)
        {
            AssessmentConfig.Scores["CustomInstructions"] += 4;
            AssessmentConfig.Findings["CustomInstructions"].Strengths.Add($"Custom instructions file found: {Path.GetFileName(foundPath)}");

            var content = File.ReadAllText(foundPath);
            var lines = content.Split('\n').Length;

            // Content length scoring - comprehensive instructions are more valuable
            if (lines >= 100)
            {
                AssessmentConfig.Scores["CustomInstructions"] += 3;
                AssessmentConfig.Findings["CustomInstructions"].Strengths.Add("Comprehensive instructions (100+ lines)");
            }
            else if (lines >= 50)
            {
                AssessmentConfig.Scores["CustomInstructions"] += 2;
                AssessmentConfig.Findings["CustomInstructions"].Strengths.Add("Good instruction coverage");
            }

            // Check for key sections - expanded list
            var keySections = new[] 
            { 
                "coding standards", "naming", "testing", "security", "architecture",
                "error handling", "logging", "dependencies", "patterns", "style"
            };
            int sectionsFound = 0;
            foreach (var section in keySections)
            {
                if (content.Contains(section, StringComparison.OrdinalIgnoreCase))
                    sectionsFound++;
            }

            int sectionScore = Math.Min(sectionsFound, 6);
            AssessmentConfig.Scores["CustomInstructions"] += sectionScore;
            if (sectionsFound >= 5)
                AssessmentConfig.Findings["CustomInstructions"].Strengths.Add($"Covers {sectionsFound} key topics (coding standards, naming, etc.)");
            else if (sectionsFound >= 3)
                AssessmentConfig.Findings["CustomInstructions"].Strengths.Add($"Covers {sectionsFound} topics");
            else
                AssessmentConfig.Findings["CustomInstructions"].Recommendations.Add("Add more sections: coding standards, naming, testing, security, error handling");

            // Check for code examples in instructions
            if (content.Contains("```"))
            {
                AssessmentConfig.Scores["CustomInstructions"] += 2;
                AssessmentConfig.Findings["CustomInstructions"].Strengths.Add("Includes code examples");
            }

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
            AssessmentConfig.Findings["CustomInstructions"].Recommendations.Add("Include: coding standards, naming conventions, testing practices, security guidelines");
        }
    }
}