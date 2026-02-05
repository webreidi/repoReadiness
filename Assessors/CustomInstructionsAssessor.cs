using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using RepoReadiness.Configuration;
using RepoReadiness.Services;

namespace RepoReadiness.Assessors;

/// <summary>
/// Assesses custom instructions - these directly tell Copilot how to behave in this repo.
/// Also detects conflicting instruction files which can cause issues with Copilot.
/// </summary>
public class CustomInstructionsAssessor : IAssessor
{
    public string CategoryName => "CustomInstructions";
    public int MaxScore => 20;

    public void Assess()
    {
        Console.WriteLine("[7/11] Assessing Custom Instructions...");

        // All possible instruction file locations that Copilot may recognize
        var instructionPaths = new[]
        {
            Path.Combine(AssessmentConfig.RepoPath, ".github", "copilot-instructions.md"),
            Path.Combine(AssessmentConfig.RepoPath, ".copilot-instructions.md"),
            Path.Combine(AssessmentConfig.RepoPath, "COPILOT.md")
        };

        // Check for conflicting instruction files (multiple files present)
        var existingFiles = instructionPaths.Where(File.Exists).ToList();
        
        if (existingFiles.Count > 1)
        {
            // Multiple instruction files detected - this causes problems with Copilot
            var fileNames = existingFiles.Select(p => Path.GetFileName(p)).ToList();
            AssessmentConfig.Findings["CustomInstructions"].Weaknesses.Add(
                $"CONFLICT: Multiple instruction files detected: {string.Join(", ", fileNames)}");
            AssessmentConfig.Findings["CustomInstructions"].Recommendations.Add(
                "Remove duplicate instruction files - keep only .github/copilot-instructions.md (recommended)");
            AssessmentConfig.Findings["CustomInstructions"].Recommendations.Add(
                "Multiple instruction files can cause unpredictable Copilot behavior and request handling issues");
        }

        string? foundPath = existingFiles.FirstOrDefault();

        if (foundPath != null)
        {
            // Award points but reduce if there are conflicts (penalty of 2 points)
            int basePoints = existingFiles.Count > 1 ? 2 : 4;
            AssessmentConfig.Scores["CustomInstructions"] += basePoints;
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

            // Check for contradictions between files if multiple exist
            if (existingFiles.Count > 1)
            {
                CheckForContradictions(existingFiles);
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

    /// <summary>
    /// Checks for potential contradictions between multiple instruction files.
    /// </summary>
    private static void CheckForContradictions(List<string> files)
    {
        try
        {
            var contents = files.Select(f => (Path: f, Content: File.ReadAllText(f))).ToList();
            
            // Check for conflicting patterns (simple heuristic checks)
            var conflictIndicators = new[]
            {
                ("tabs", "spaces"),
                ("single quotes", "double quotes"),
                ("camelCase", "PascalCase"),
                ("snake_case", "camelCase"),
                ("async/await", "callbacks"),
                ("classes", "functions")
            };

            foreach (var (term1, term2) in conflictIndicators)
            {
                if (contents.Count >= 2)
                {
                    bool file1HasTerm1 = contents[0].Content.Contains(term1, StringComparison.OrdinalIgnoreCase);
                    bool file1HasTerm2 = contents[0].Content.Contains(term2, StringComparison.OrdinalIgnoreCase);
                    bool file2HasTerm1 = contents[1].Content.Contains(term1, StringComparison.OrdinalIgnoreCase);
                    bool file2HasTerm2 = contents[1].Content.Contains(term2, StringComparison.OrdinalIgnoreCase);

                    // Potential conflict: one file mentions term1, other mentions term2 (but not both)
                    if ((file1HasTerm1 && file2HasTerm2 && !file1HasTerm2 && !file2HasTerm1) ||
                        (file1HasTerm2 && file2HasTerm1 && !file1HasTerm1 && !file2HasTerm2))
                    {
                        AssessmentConfig.Findings["CustomInstructions"].Weaknesses.Add(
                            $"Potential conflict: '{term1}' vs '{term2}' mentioned in different files");
                    }
                }
            }
        }
        catch
        {
            // Silently ignore errors in contradiction detection
        }
    }
}