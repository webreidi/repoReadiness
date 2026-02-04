using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RepoReadiness.Configuration;

namespace RepoReadiness.Assessors;

/// <summary>
/// Assesses type safety - typed code gives Copilot much better context for suggestions.
/// </summary>
public class TypeSafetyAssessor : IAssessor
{
    public string CategoryName => "TypeSafety";
    public int MaxScore => 10;

    public void Assess()
    {
        Console.WriteLine("[8/11] Assessing Type Safety...");

        bool hasTyping = false;
        bool hasStrictMode = false;

        // Check for TypeScript
        var tsConfig = Path.Combine(AssessmentConfig.RepoPath, "tsconfig.json");
        if (File.Exists(tsConfig))
        {
            hasTyping = true;
            AssessmentConfig.Scores["TypeSafety"] += 4;
            AssessmentConfig.Findings["TypeSafety"].Strengths.Add("TypeScript configured (tsconfig.json)");

            // Check for strict mode
            var content = File.ReadAllText(tsConfig);
            if (content.Contains("\"strict\": true") || content.Contains("\"strict\":true"))
            {
                hasStrictMode = true;
                AssessmentConfig.Scores["TypeSafety"] += 3;
                AssessmentConfig.Findings["TypeSafety"].Strengths.Add("TypeScript strict mode enabled");
            }
            else
            {
                AssessmentConfig.Findings["TypeSafety"].Recommendations.Add("Enable TypeScript strict mode for better Copilot suggestions");
            }
        }

        // Check for TypeScript files
        var tsFiles = Directory.GetFiles(AssessmentConfig.RepoPath, "*.ts", SearchOption.AllDirectories)
            .Where(f => !f.Contains("node_modules") && !f.Contains(".d.ts")).ToList();
        var jsFiles = Directory.GetFiles(AssessmentConfig.RepoPath, "*.js", SearchOption.AllDirectories)
            .Where(f => !f.Contains("node_modules")).ToList();

        if (tsFiles.Any() && !hasTyping)
        {
            hasTyping = true;
            AssessmentConfig.Scores["TypeSafety"] += 3;
            AssessmentConfig.Findings["TypeSafety"].Strengths.Add($"TypeScript files found ({tsFiles.Count} files)");
        }

        // Check TypeScript/JavaScript ratio
        if (tsFiles.Any() && jsFiles.Any())
        {
            double ratio = (double)tsFiles.Count / (tsFiles.Count + jsFiles.Count);
            if (ratio >= 0.8)
            {
                AssessmentConfig.Scores["TypeSafety"] += 2;
                AssessmentConfig.Findings["TypeSafety"].Strengths.Add($"High TypeScript coverage ({ratio:P0})");
            }
            else if (ratio < 0.5)
            {
                AssessmentConfig.Findings["TypeSafety"].Recommendations.Add("Consider migrating more JavaScript files to TypeScript");
            }
        }

        // Check for Python type hints
        var pyFiles = Directory.GetFiles(AssessmentConfig.RepoPath, "*.py", SearchOption.AllDirectories)
            .Where(f => !f.Contains("venv") && !f.Contains("__pycache__")).Take(10).ToList();
        
        if (pyFiles.Any())
        {
            int filesWithTypeHints = 0;
            foreach (var file in pyFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    // Check for type hints: def func(x: int) -> str: or variable: Type
                    if (Regex.IsMatch(content, @":\s*(int|str|float|bool|List|Dict|Optional|Union|Any)\b") ||
                        Regex.IsMatch(content, @"->\s*(int|str|float|bool|List|Dict|Optional|None)"))
                    {
                        filesWithTypeHints++;
                    }
                }
                catch { }
            }

            if (filesWithTypeHints >= pyFiles.Count / 2)
            {
                hasTyping = true;
                AssessmentConfig.Scores["TypeSafety"] += 4;
                AssessmentConfig.Findings["TypeSafety"].Strengths.Add("Python type hints used");
            }
            else if (filesWithTypeHints > 0)
            {
                AssessmentConfig.Scores["TypeSafety"] += 2;
                AssessmentConfig.Findings["TypeSafety"].Strengths.Add("Some Python type hints present");
                AssessmentConfig.Findings["TypeSafety"].Recommendations.Add("Add type hints to more Python files for better Copilot suggestions");
            }
            else
            {
                AssessmentConfig.Findings["TypeSafety"].Recommendations.Add("Add Python type hints (def func(x: int) -> str:) for better Copilot context");
            }

            // Check for mypy or pyright config
            var typeCheckers = new[] { "mypy.ini", ".mypy.ini", "pyrightconfig.json", "pyproject.toml" };
            foreach (var checker in typeCheckers)
            {
                var path = Path.Combine(AssessmentConfig.RepoPath, checker);
                if (File.Exists(path))
                {
                    var content = File.ReadAllText(path);
                    if (checker == "pyproject.toml" && !content.Contains("[tool.mypy]") && !content.Contains("[tool.pyright]"))
                        continue;
                    
                    hasStrictMode = true;
                    AssessmentConfig.Scores["TypeSafety"] += 2;
                    AssessmentConfig.Findings["TypeSafety"].Strengths.Add($"Python type checker configured: {checker}");
                    break;
                }
            }
        }

        // Check for C# nullable reference types
        var csprojFiles = Directory.GetFiles(AssessmentConfig.RepoPath, "*.csproj", SearchOption.AllDirectories);
        foreach (var csproj in csprojFiles)
        {
            try
            {
                var content = File.ReadAllText(csproj);
                if (content.Contains("<Nullable>enable</Nullable>"))
                {
                    hasTyping = true;
                    AssessmentConfig.Scores["TypeSafety"] += 4;
                    AssessmentConfig.Findings["TypeSafety"].Strengths.Add("C# nullable reference types enabled");
                    break;
                }
            }
            catch { }
        }

        // If no typing found at all
        if (!hasTyping)
        {
            AssessmentConfig.Findings["TypeSafety"].Weaknesses.Add("No type safety features detected");
            AssessmentConfig.Findings["TypeSafety"].Recommendations.Add("Consider using TypeScript, Python type hints, or C# nullable types for better Copilot suggestions");
        }
    }
}
