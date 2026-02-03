using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RepoReadiness.Configuration;

namespace RepoReadiness.Assessors;

/// <summary>
/// Assesses documentation quality - critical for Copilot to understand project context.
/// </summary>
public class DocumentationAssessor : IAssessor
{
    public string CategoryName => "Documentation";
    public int MaxScore => 25;

    public void Assess()
    {
        Console.WriteLine("[5/10] Assessing Documentation Quality...");

        // Check README - expanded scoring
        var readmePath = Path.Combine(AssessmentConfig.RepoPath, "README.md");
        if (File.Exists(readmePath))
        {
            var content = File.ReadAllText(readmePath);
            var lines = content.Split('\n').Length;

            // Base score for README presence and length
            if (lines >= 100)
            {
                AssessmentConfig.Scores["Documentation"] += 6;
                AssessmentConfig.Findings["Documentation"].Strengths.Add("Comprehensive README.md (100+ lines)");
            }
            else if (lines >= 50)
            {
                AssessmentConfig.Scores["Documentation"] += 4;
                AssessmentConfig.Findings["Documentation"].Strengths.Add("Good README.md coverage");
            }
            else if (lines >= 20)
            {
                AssessmentConfig.Scores["Documentation"] += 2;
                AssessmentConfig.Findings["Documentation"].Strengths.Add("README.md present with basic content");
            }
            else
            {
                AssessmentConfig.Scores["Documentation"] += 1;
                AssessmentConfig.Findings["Documentation"].Weaknesses.Add("README.md is minimal");
                AssessmentConfig.Findings["Documentation"].Recommendations.Add("Expand README with setup, usage, and examples");
            }

            // Check for key README sections
            var keySections = new[] { "install", "usage", "example", "api", "contributing", "license" };
            int sectionsFound = keySections.Count(s => content.Contains(s, StringComparison.OrdinalIgnoreCase));
            if (sectionsFound >= 4)
            {
                AssessmentConfig.Scores["Documentation"] += 3;
                AssessmentConfig.Findings["Documentation"].Strengths.Add($"README covers {sectionsFound} key sections");
            }
            else if (sectionsFound >= 2)
            {
                AssessmentConfig.Scores["Documentation"] += 1;
            }
        }
        else
        {
            AssessmentConfig.Findings["Documentation"].Weaknesses.Add("No README.md found");
            AssessmentConfig.Findings["Documentation"].Recommendations.Add("Add a README.md with project description, setup, and usage");
        }

        // Check for additional documentation
        var docsDirs = new[] { "docs", "doc", "documentation", "wiki" };
        foreach (var dir in docsDirs)
        {
            if (Directory.Exists(Path.Combine(AssessmentConfig.RepoPath, dir)))
            {
                AssessmentConfig.Scores["Documentation"] += 3;
                AssessmentConfig.Findings["Documentation"].Strengths.Add($"Documentation directory found: {dir}/");
                break;
            }
        }

        // Check for additional markdown files
        var mdFiles = Directory.GetFiles(AssessmentConfig.RepoPath, "*.md", SearchOption.TopDirectoryOnly)
            .Where(f => !Path.GetFileName(f).Equals("README.md", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (mdFiles.Count >= 3)
        {
            AssessmentConfig.Scores["Documentation"] += 3;
            AssessmentConfig.Findings["Documentation"].Strengths.Add($"Rich documentation: {string.Join(", ", mdFiles.Select(Path.GetFileName).Take(3))}");
        }
        else if (mdFiles.Count >= 1)
        {
            AssessmentConfig.Scores["Documentation"] += 2;
            AssessmentConfig.Findings["Documentation"].Strengths.Add($"Additional docs: {string.Join(", ", mdFiles.Select(Path.GetFileName))}");
        }

        // Check for API documentation
        var apiDocs = new[] { "swagger.json", "openapi.json", "openapi.yaml", "api.md" };
        foreach (var apiDoc in apiDocs)
        {
            var files = Directory.GetFiles(AssessmentConfig.RepoPath, apiDoc, SearchOption.AllDirectories);
            if (files.Any())
            {
                AssessmentConfig.Scores["Documentation"] += 3;
                AssessmentConfig.Findings["Documentation"].Strengths.Add($"API documentation found: {apiDoc}");
                break;
            }
        }

        // Check for architecture documentation
        var archDocs = new[] { "ARCHITECTURE.md", "DESIGN.md", "architecture.md", "design.md" };
        foreach (var archDoc in archDocs)
        {
            if (File.Exists(Path.Combine(AssessmentConfig.RepoPath, archDoc)))
            {
                AssessmentConfig.Scores["Documentation"] += 2;
                AssessmentConfig.Findings["Documentation"].Strengths.Add($"Architecture documentation: {archDoc}");
                break;
            }
        }

        // Check for inline comments quality (sample analysis)
        var codeFiles = Directory.GetFiles(AssessmentConfig.RepoPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj") && !f.Contains("bin")).Take(5).ToList();

        int filesWithXmlDocs = 0;
        int filesWithComments = 0;
        foreach (var file in codeFiles)
        {
            try
            {
                var content = File.ReadAllText(file);
                if (content.Contains("///") || content.Contains("<summary>"))
                    filesWithXmlDocs++;
                if (Regex.IsMatch(content, @"//\s*\w") || content.Contains("/*"))
                    filesWithComments++;
            }
            catch { }
        }

        if (codeFiles.Any())
        {
            if (filesWithXmlDocs >= codeFiles.Count / 2)
            {
                AssessmentConfig.Scores["Documentation"] += 3;
                AssessmentConfig.Findings["Documentation"].Strengths.Add("XML documentation comments present");
            }
            else if (filesWithComments >= codeFiles.Count / 2)
            {
                AssessmentConfig.Scores["Documentation"] += 2;
                AssessmentConfig.Findings["Documentation"].Strengths.Add("Code contains inline comments");
            }
            else
            {
                AssessmentConfig.Findings["Documentation"].Recommendations.Add("Add XML documentation (///) to public APIs for better Copilot context");
            }
        }
    }
}