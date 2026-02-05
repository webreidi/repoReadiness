using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RepoReadiness.Configuration;

namespace RepoReadiness.Assessors;

/// <summary>
/// Assesses how friendly the codebase is for Copilot's context window.
/// Smaller files, clear imports, and proper exclusions help Copilot provide better suggestions.
/// </summary>
public class ContextFriendlinessAssessor : IAssessor
{
    public string CategoryName => "ContextFriendliness";
    public int MaxScore => 25;

    public void Assess()
    {
        Console.WriteLine("[8/10] Assessing Context Friendliness...");

        // Check average file size
        var codeExtensions = new[] { ".cs", ".js", ".ts", ".py", ".java", ".go", ".rs", ".tsx", ".jsx" };
        var fileSizes = new List<int>();

        foreach (var ext in codeExtensions)
        {
            try
            {
                var files = Directory.GetFiles(AssessmentConfig.RepoPath, $"*{ext}", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("node_modules") && !f.Contains(".git") && 
                               !f.Contains("bin") && !f.Contains("obj") && !f.Contains("dist"));
                
                foreach (var file in files)
                {
                    try
                    {
                        var lines = File.ReadAllLines(file).Length;
                        fileSizes.Add(lines);
                    }
                    catch { }
                }
            }
            catch { }
        }

        if (fileSizes.Any())
        {
            double avgSize = fileSizes.Average();
            int filesUnder300 = fileSizes.Count(s => s <= 300);
            double pctUnder300 = (double)filesUnder300 / fileSizes.Count;

            // File size scoring - critical for AI context window (increased weight)
            if (avgSize <= 200)
            {
                AssessmentConfig.Scores["ContextFriendliness"] += 8;
                AssessmentConfig.Findings["ContextFriendliness"].Strengths.Add($"Excellent file sizes (avg {avgSize:F0} lines)");
            }
            else if (avgSize <= 300)
            {
                AssessmentConfig.Scores["ContextFriendliness"] += 5;
                AssessmentConfig.Findings["ContextFriendliness"].Strengths.Add($"Good file sizes (avg {avgSize:F0} lines)");
            }
            else
            {
                AssessmentConfig.Findings["ContextFriendliness"].Weaknesses.Add($"Large average file size ({avgSize:F0} lines)");
                AssessmentConfig.Findings["ContextFriendliness"].Recommendations.Add("Split large files into smaller modules for better Copilot context");
            }

            if (pctUnder300 >= 0.8)
            {
                AssessmentConfig.Scores["ContextFriendliness"] += 3;
                AssessmentConfig.Findings["ContextFriendliness"].Strengths.Add($"{pctUnder300:P0} of files are under 300 lines");
            }
        }

        // Check .gitignore configuration - increased weight
        var gitignorePath = Path.Combine(AssessmentConfig.RepoPath, ".gitignore");
        if (File.Exists(gitignorePath))
        {
            var content = File.ReadAllText(gitignorePath);
            var essentialExcludes = new[] { "node_modules", "bin", "obj", ".env", "dist", "__pycache__", "venv" };
            int excludesFound = essentialExcludes.Count(e => content.Contains(e, StringComparison.OrdinalIgnoreCase));

            if (excludesFound >= 3)
            {
                AssessmentConfig.Scores["ContextFriendliness"] += 4;
                AssessmentConfig.Findings["ContextFriendliness"].Strengths.Add(".gitignore properly configured");
            }
            else
            {
                AssessmentConfig.Scores["ContextFriendliness"] += 2;
                AssessmentConfig.Findings["ContextFriendliness"].Recommendations.Add("Ensure .gitignore excludes build artifacts and dependencies");
            }
        }
        else
        {
            AssessmentConfig.Findings["ContextFriendliness"].Weaknesses.Add("No .gitignore found");
            AssessmentConfig.Findings["ContextFriendliness"].Recommendations.Add("Add .gitignore to exclude build artifacts and dependencies");
        }

        // Check for .copilotignore - important for focused context
        var copilotIgnorePath = Path.Combine(AssessmentConfig.RepoPath, ".copilotignore");
        if (File.Exists(copilotIgnorePath))
        {
            AssessmentConfig.Scores["ContextFriendliness"] += 4;
            AssessmentConfig.Findings["ContextFriendliness"].Strengths.Add(".copilotignore configured for focused context");
        }

        // Check for minified/bundled files in source - increased weight
        var problematicFiles = new List<string>();
        var minPatterns = new[] { "*.min.js", "*.bundle.js", "*.min.css" };
        foreach (var pattern in minPatterns)
        {
            try
            {
                var files = Directory.GetFiles(AssessmentConfig.RepoPath, pattern, SearchOption.AllDirectories)
                    .Where(f => !f.Contains("node_modules") && !f.Contains("dist") && !f.Contains("build"));
                problematicFiles.AddRange(files.Select(Path.GetFileName));
            }
            catch { }
        }

        if (!problematicFiles.Any())
        {
            AssessmentConfig.Scores["ContextFriendliness"] += 3;
            AssessmentConfig.Findings["ContextFriendliness"].Strengths.Add("No minified/bundled files in source");
        }
        else
        {
            AssessmentConfig.Findings["ContextFriendliness"].Weaknesses.Add($"Minified files in source: {string.Join(", ", problematicFiles.Take(3))}");
            AssessmentConfig.Findings["ContextFriendliness"].Recommendations.Add("Move minified files to dist/ or exclude from Copilot context");
        }

        // Check for reasonable directory depth - increased weight
        try
        {
            var allFiles = Directory.GetFiles(AssessmentConfig.RepoPath, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.Contains("node_modules") && !f.Contains(".git"));
            
            int maxDepth = 0;
            foreach (var file in allFiles.Take(100))
            {
                var relativePath = Path.GetRelativePath(AssessmentConfig.RepoPath, file);
                int depth = relativePath.Count(c => c == Path.DirectorySeparatorChar);
                if (depth > maxDepth) maxDepth = depth;
            }

            if (maxDepth <= 5)
            {
                AssessmentConfig.Scores["ContextFriendliness"] += 3;
                AssessmentConfig.Findings["ContextFriendliness"].Strengths.Add($"Reasonable directory depth (max {maxDepth} levels)");
            }
            else
            {
                AssessmentConfig.Findings["ContextFriendliness"].Recommendations.Add($"Deep directory structure ({maxDepth} levels) may make navigation harder");
            }
        }
        catch { }
    }
}
