using System;
using System.IO;
using System.Linq;
using RepoReadiness.Configuration;

namespace RepoReadiness.Assessors;

public class DocumentationAssessor : IAssessor
{
    public string CategoryName => "Documentation";
    public int MaxScore => 15;

    public void Assess()
    {
        Console.WriteLine("[5/8] Assessing Documentation Quality...");

        // Check README
        var readmePath = Path.Combine(AssessmentConfig.RepoPath, "README.md");
        if (File.Exists(readmePath))
        {
            var content = File.ReadAllText(readmePath);
            var lines = content.Split('\n').Length;

            if (lines >= 50)
            {
                AssessmentConfig.Scores["Documentation"] += 5;
                AssessmentConfig.Findings["Documentation"].Strengths.Add("Comprehensive README.md");
            }
            else if (lines >= 20)
            {
                AssessmentConfig.Scores["Documentation"] += 3;
                AssessmentConfig.Findings["Documentation"].Strengths.Add("README.md present with basic content");
            }
            else
            {
                AssessmentConfig.Scores["Documentation"] += 1;
                AssessmentConfig.Findings["Documentation"].Weaknesses.Add("README.md is minimal");
                AssessmentConfig.Findings["Documentation"].Recommendations.Add("Expand README with setup, usage, and examples");
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

        if (mdFiles.Count >= 2)
        {
            AssessmentConfig.Scores["Documentation"] += 3;
            AssessmentConfig.Findings["Documentation"].Strengths.Add($"Additional documentation: {string.Join(", ", mdFiles.Select(Path.GetFileName).Take(3))}");
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

        // Check for inline comments (sample check)
        var codeFiles = Directory.GetFiles(AssessmentConfig.RepoPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj") && !f.Contains("bin")).Take(5).ToList();

        int filesWithComments = 0;
        foreach (var file in codeFiles)
        {
            try
            {
                var content = File.ReadAllText(file);
                if (content.Contains("///") || content.Contains("//") || content.Contains("/*"))
                    filesWithComments++;
            }
            catch { }
        }

        if (codeFiles.Any() && filesWithComments >= codeFiles.Count / 2)
        {
            AssessmentConfig.Scores["Documentation"] += 1;
            AssessmentConfig.Findings["Documentation"].Strengths.Add("Code contains inline comments");
        }
    }
}