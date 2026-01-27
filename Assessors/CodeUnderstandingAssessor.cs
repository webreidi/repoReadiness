using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RepoReadiness.Configuration;

namespace RepoReadiness.Assessors;

public class CodeUnderstandingAssessor : IAssessor
{
    public string CategoryName => "CodeUnderstanding";
    public int MaxScore => 20;

    public void Assess()
    {
        Console.WriteLine("[4/8] Assessing Code Understanding...");

        // Check directory structure (not too flat, not too deep)
        var allDirs = Directory.GetDirectories(AssessmentConfig.RepoPath, "*", SearchOption.AllDirectories)
            .Where(d => !d.Contains("node_modules") && !d.Contains(".git") && !d.Contains("bin") && !d.Contains("obj"))
            .ToList();

        if (allDirs.Count >= 3 && allDirs.Count <= 50)
        {
            AssessmentConfig.Scores["CodeUnderstanding"] += 5;
            AssessmentConfig.Findings["CodeUnderstanding"].Strengths.Add("Well-organized directory structure");
        }
        else if (allDirs.Count < 3)
        {
            AssessmentConfig.Findings["CodeUnderstanding"].Recommendations.Add("Consider organizing code into subdirectories");
        }
        else
        {
            AssessmentConfig.Scores["CodeUnderstanding"] += 2;
            AssessmentConfig.Findings["CodeUnderstanding"].Weaknesses.Add("Complex directory structure may be hard to navigate");
        }

        // Check for oversized files
        var codeExtensions = new[] { ".cs", ".js", ".ts", ".py", ".java", ".go", ".rs" };
        var largeFiles = new List<string>();
        foreach (var ext in codeExtensions)
        {
            try
            {
                var files = Directory.GetFiles(AssessmentConfig.RepoPath, $"*{ext}", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("node_modules") && !f.Contains(".git"));
                foreach (var file in files)
                {
                    var lines = File.ReadAllLines(file).Length;
                    if (lines > 500)
                        largeFiles.Add($"{Path.GetFileName(file)} ({lines} lines)");
                }
            }
            catch { }
        }

        if (largeFiles.Count == 0)
        {
            AssessmentConfig.Scores["CodeUnderstanding"] += 5;
            AssessmentConfig.Findings["CodeUnderstanding"].Strengths.Add("No oversized files detected");
        }
        else
        {
            AssessmentConfig.Findings["CodeUnderstanding"].Weaknesses.Add($"Large files detected: {string.Join(", ", largeFiles.Take(3))}");
            AssessmentConfig.Findings["CodeUnderstanding"].Recommendations.Add("Consider splitting large files into smaller modules");
        }

        // Check for TypeScript or typed languages
        var tsFiles = Directory.GetFiles(AssessmentConfig.RepoPath, "*.ts", SearchOption.AllDirectories)
            .Where(f => !f.Contains("node_modules")).ToList();
        var tsConfig = Path.Combine(AssessmentConfig.RepoPath, "tsconfig.json");
        if (tsFiles.Any() || File.Exists(tsConfig))
        {
            AssessmentConfig.Scores["CodeUnderstanding"] += 4;
            AssessmentConfig.Findings["CodeUnderstanding"].Strengths.Add("TypeScript provides type safety");
        }

        // Check for linting configuration
        var lintConfigs = new[] { ".eslintrc", ".eslintrc.js", ".eslintrc.json", ".editorconfig", ".prettierrc" };
        foreach (var config in lintConfigs)
        {
            if (File.Exists(Path.Combine(AssessmentConfig.RepoPath, config)))
            {
                AssessmentConfig.Scores["CodeUnderstanding"] += 4;
                AssessmentConfig.Findings["CodeUnderstanding"].Strengths.Add($"Code style enforced: {config}");
                break;
            }
        }

        // Check for consistent naming (sample check)
        var csFiles = Directory.GetFiles(AssessmentConfig.RepoPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj") && !f.Contains("bin")).Take(10).ToList();
        if (csFiles.Any())
        {
            bool hasPascalCase = csFiles.All(f => char.IsUpper(Path.GetFileNameWithoutExtension(f)[0]));
            if (hasPascalCase)
            {
                AssessmentConfig.Scores["CodeUnderstanding"] += 2;
                AssessmentConfig.Findings["CodeUnderstanding"].Strengths.Add("Consistent PascalCase naming for C# files");
            }
        }
    }
}