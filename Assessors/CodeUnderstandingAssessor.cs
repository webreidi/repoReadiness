using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RepoReadiness.Configuration;

namespace RepoReadiness.Assessors;

/// <summary>
/// Assesses code quality factors that directly impact Copilot's ability to understand
/// and generate high-quality code suggestions.
/// </summary>
public class CodeQualityAssessor : IAssessor
{
    public string CategoryName => "CodeQuality";
    public int MaxScore => 30;

    public void Assess()
    {
        Console.WriteLine("[4/10] Assessing Code Quality...");

        // Check directory structure (not too flat, not too deep)
        var allDirs = Directory.GetDirectories(AssessmentConfig.RepoPath, "*", SearchOption.AllDirectories)
            .Where(d => !d.Contains("node_modules") && !d.Contains(".git") && !d.Contains("bin") && !d.Contains("obj"))
            .ToList();

        if (allDirs.Count >= 3 && allDirs.Count <= 50)
        {
            AssessmentConfig.Scores["CodeQuality"] += 5;
            AssessmentConfig.Findings["CodeQuality"].Strengths.Add("Well-organized directory structure");
        }
        else if (allDirs.Count < 3)
        {
            AssessmentConfig.Findings["CodeQuality"].Recommendations.Add("Consider organizing code into subdirectories");
        }
        else
        {
            AssessmentConfig.Scores["CodeQuality"] += 2;
            AssessmentConfig.Findings["CodeQuality"].Weaknesses.Add("Complex directory structure may be hard to navigate");
        }

        // Check for oversized files (Copilot struggles with large files)
        var codeExtensions = new[] { ".cs", ".js", ".ts", ".py", ".java", ".go", ".rs" };
        var largeFiles = new List<string>();
        var allCodeFiles = new List<string>();
        
        foreach (var ext in codeExtensions)
        {
            try
            {
                var files = Directory.GetFiles(AssessmentConfig.RepoPath, $"*{ext}", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("node_modules") && !f.Contains(".git") && !f.Contains("bin") && !f.Contains("obj"));
                foreach (var file in files)
                {
                    allCodeFiles.Add(file);
                    var lines = File.ReadAllLines(file).Length;
                    if (lines > 500)
                        largeFiles.Add($"{Path.GetFileName(file)} ({lines} lines)");
                }
            }
            catch { }
        }

        if (largeFiles.Count == 0)
        {
            AssessmentConfig.Scores["CodeQuality"] += 5;
            AssessmentConfig.Findings["CodeQuality"].Strengths.Add("No oversized files detected (all <500 lines)");
        }
        else
        {
            AssessmentConfig.Findings["CodeQuality"].Weaknesses.Add($"Large files detected: {string.Join(", ", largeFiles.Take(3))}");
            AssessmentConfig.Findings["CodeQuality"].Recommendations.Add("Consider splitting large files into smaller modules for better Copilot context");
        }

        // Check for consistent naming conventions
        var csFiles = Directory.GetFiles(AssessmentConfig.RepoPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj") && !f.Contains("bin")).Take(10).ToList();
        if (csFiles.Any())
        {
            bool hasPascalCase = csFiles.All(f => char.IsUpper(Path.GetFileNameWithoutExtension(f)[0]));
            if (hasPascalCase)
            {
                AssessmentConfig.Scores["CodeQuality"] += 4;
                AssessmentConfig.Findings["CodeQuality"].Strengths.Add("Consistent PascalCase naming for C# files");
            }
        }

        // Check for linting/formatting configuration
        var lintConfigs = new[] { ".eslintrc", ".eslintrc.js", ".eslintrc.json", ".editorconfig", ".prettierrc", "stylecop.json", ".globalconfig" };
        foreach (var config in lintConfigs)
        {
            if (File.Exists(Path.Combine(AssessmentConfig.RepoPath, config)))
            {
                AssessmentConfig.Scores["CodeQuality"] += 4;
                AssessmentConfig.Findings["CodeQuality"].Strengths.Add($"Code style enforced: {config}");
                break;
            }
        }

        // Check for clear separation of concerns (services/models/controllers pattern)
        var separationDirs = new[] { "Services", "Models", "Controllers", "Repositories", "Handlers", "src", "lib" };
        int foundSeparation = 0;
        foreach (var dir in separationDirs)
        {
            if (Directory.Exists(Path.Combine(AssessmentConfig.RepoPath, dir)))
                foundSeparation++;
        }
        if (foundSeparation >= 2)
        {
            AssessmentConfig.Scores["CodeQuality"] += 4;
            AssessmentConfig.Findings["CodeQuality"].Strengths.Add("Clear separation of concerns (organized folders)");
        }

        // Check function/method sizes (sample analysis)
        int wellSizedMethods = 0;
        int oversizedMethods = 0;
        foreach (var file in allCodeFiles.Take(5))
        {
            try
            {
                var content = File.ReadAllText(file);
                // Simple heuristic: count methods by looking for method signatures
                var methodMatches = Regex.Matches(content, @"(public|private|protected|internal)\s+\w+\s+\w+\s*\([^)]*\)\s*\{", RegexOptions.Multiline);
                foreach (Match match in methodMatches)
                {
                    // Find matching closing brace (simplified)
                    int braceCount = 1;
                    int methodLength = 0;
                    for (int i = match.Index + match.Length; i < content.Length && braceCount > 0; i++)
                    {
                        if (content[i] == '{') braceCount++;
                        if (content[i] == '}') braceCount--;
                        if (content[i] == '\n') methodLength++;
                    }
                    if (methodLength <= 50) wellSizedMethods++;
                    else oversizedMethods++;
                }
            }
            catch { }
        }
        if (wellSizedMethods > oversizedMethods && wellSizedMethods > 0)
        {
            AssessmentConfig.Scores["CodeQuality"] += 4;
            AssessmentConfig.Findings["CodeQuality"].Strengths.Add("Methods are reasonably sized (<50 lines)");
        }
        else if (oversizedMethods > 0)
        {
            AssessmentConfig.Findings["CodeQuality"].Recommendations.Add("Consider breaking up large methods for better Copilot suggestions");
        }

        // Check import organization (are imports at top of files?)
        int organizedImports = 0;
        foreach (var file in allCodeFiles.Take(5))
        {
            try
            {
                var lines = File.ReadAllLines(file);
                bool importsAtTop = true;
                bool passedImports = false;
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//") || trimmed.StartsWith("/*")) continue;
                    
                    bool isImport = trimmed.StartsWith("using ") || trimmed.StartsWith("import ") || 
                                    trimmed.StartsWith("from ") || trimmed.StartsWith("require(");
                    if (isImport && passedImports)
                    {
                        importsAtTop = false;
                        break;
                    }
                    if (!isImport && !trimmed.StartsWith("namespace") && !trimmed.StartsWith("package"))
                        passedImports = true;
                }
                if (importsAtTop) organizedImports++;
            }
            catch { }
        }
        if (organizedImports >= 3)
        {
            AssessmentConfig.Scores["CodeQuality"] += 4;
            AssessmentConfig.Findings["CodeQuality"].Strengths.Add("Well-organized imports at file tops");
        }
    }
}