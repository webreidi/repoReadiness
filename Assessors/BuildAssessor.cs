using System;
using System.IO;
using System.Linq;
using RepoReadiness.Configuration;
using RepoReadiness.Services;

namespace RepoReadiness.Assessors;

public class BuildAssessor : IAssessor
{
    public string CategoryName => "Build";
    public int MaxScore => 20;

    public void Assess()
    {
        Console.WriteLine("[1/11] Assessing Build Capability...");

        var buildPatterns = new[] { "*.csproj", "*.sln", "package.json", "Cargo.toml", "pom.xml", "build.gradle", "Makefile", "CMakeLists.txt" };
        bool hasBuildConfig = false;
        string? detectedBuildFile = null;

        foreach (var pattern in buildPatterns)
        {
            try
            {
                var files = Directory.GetFiles(AssessmentConfig.RepoPath, pattern, SearchOption.AllDirectories);
                if (files.Any())
                {
                    hasBuildConfig = true;
                    detectedBuildFile = Path.GetFileName(files.First());
                    AssessmentConfig.Scores["Build"] += 5;
                    AssessmentConfig.Findings["Build"].Strengths.Add($"Build configuration found: {detectedBuildFile}");
                    break;
                }
            }
            catch { }
        }

        if (!hasBuildConfig)
        {
            AssessmentConfig.Findings["Build"].Weaknesses.Add("No build configuration file detected");
            AssessmentConfig.Findings["Build"].Recommendations.Add("Add a build configuration (e.g., .csproj, package.json, Makefile)");
        }

        // Check README for build instructions
        var readmePath = Path.Combine(AssessmentConfig.RepoPath, "README.md");
        if (File.Exists(readmePath))
        {
            var content = File.ReadAllText(readmePath);
            if (content.Contains("build", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("compile", StringComparison.OrdinalIgnoreCase))
            {
                AssessmentConfig.Scores["Build"] += 4;
                AssessmentConfig.Findings["Build"].Strengths.Add("README contains build instructions");
            }
            else
            {
                AssessmentConfig.Findings["Build"].Recommendations.Add("Add build instructions to README.md");
            }
        }

        // Check for CI/CD
        var ciPaths = new[] { ".github/workflows", ".gitlab-ci.yml", "azure-pipelines.yml", "Jenkinsfile", ".circleci" };
        foreach (var ciPath in ciPaths)
        {
            var fullPath = Path.Combine(AssessmentConfig.RepoPath, ciPath);
            if (Directory.Exists(fullPath) || File.Exists(fullPath))
            {
                AssessmentConfig.Scores["Build"] += 4;
                AssessmentConfig.Findings["Build"].Strengths.Add($"CI/CD configuration found: {ciPath}");
                break;
            }
        }

        // Check for build scripts
        var buildScripts = new[] { "build.ps1", "build.sh", "build.cmd", "build.bat" };
        foreach (var script in buildScripts)
        {
            if (File.Exists(Path.Combine(AssessmentConfig.RepoPath, script)))
            {
                AssessmentConfig.Scores["Build"] += 3;
                AssessmentConfig.Findings["Build"].Strengths.Add($"Build script found: {script}");
                break;
            }
        }

        // Copilot understanding test
        if (AssessmentConfig.CopilotAvailable)
        {
            string response = CopilotService.AskCopilot("What is the exact command to install dependencies and build this project? Provide only the commands.");
            if (!string.IsNullOrWhiteSpace(response) && !response.StartsWith("Error"))
            {
                var sourceContent = hasBuildConfig && detectedBuildFile != null ? detectedBuildFile : "";
                int understanding = CopilotService.EvaluateCopilotUnderstanding(response, sourceContent);
                AssessmentConfig.Scores["Build"] += Math.Min(understanding, 4);
                if (understanding >= 3)
                    AssessmentConfig.Findings["Build"].Strengths.Add("Copilot understands the build process");
            }
        }
    }
}