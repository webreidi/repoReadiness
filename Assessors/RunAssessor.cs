using System;
using System.IO;
using System.Linq;
using RepoReadiness.Configuration;
using RepoReadiness.Services;

namespace RepoReadiness.Assessors;

public class RunAssessor : IAssessor
{
    public string CategoryName => "Run";
    public int MaxScore => 15;

    public void Assess()
    {
        Console.WriteLine("[2/10] Assessing Run Capability...");

        // Check for entry points
        var entryPatterns = new[] { "Program.cs", "main.py", "index.js", "index.ts", "main.go", "Main.java", "main.rs" };
        bool hasEntryPoint = false;

        foreach (var pattern in entryPatterns)
        {
            var files = Directory.GetFiles(AssessmentConfig.RepoPath, pattern, SearchOption.AllDirectories);
            if (files.Any())
            {
                hasEntryPoint = true;
                AssessmentConfig.Scores["Run"] += 3;
                AssessmentConfig.Findings["Run"].Strengths.Add($"Entry point identified: {pattern}");
                break;
            }
        }

        if (!hasEntryPoint)
        {
            AssessmentConfig.Findings["Run"].Weaknesses.Add("No clear entry point found");
            AssessmentConfig.Findings["Run"].Recommendations.Add("Add a clear entry point file (e.g., Program.cs, main.py)");
        }

        // Check for environment configuration
        var envFiles = new[] { ".env.example", ".env.template", "appsettings.json", "config.example.json" };
        foreach (var envFile in envFiles)
        {
            if (File.Exists(Path.Combine(AssessmentConfig.RepoPath, envFile)))
            {
                AssessmentConfig.Scores["Run"] += 4;
                AssessmentConfig.Findings["Run"].Strengths.Add($"Environment template found: {envFile}");
                break;
            }
        }

        // Check for launch configurations
        var launchConfig = Path.Combine(AssessmentConfig.RepoPath, ".vscode", "launch.json");
        if (File.Exists(launchConfig))
        {
            AssessmentConfig.Scores["Run"] += 2;
            AssessmentConfig.Findings["Run"].Strengths.Add("VS Code launch configuration found");
        }

        // Check for start scripts in package.json
        var packageJson = Path.Combine(AssessmentConfig.RepoPath, "package.json");
        if (File.Exists(packageJson))
        {
            var content = File.ReadAllText(packageJson);
            if (content.Contains("\"start\""))
            {
                AssessmentConfig.Scores["Run"] += 2;
                AssessmentConfig.Findings["Run"].Strengths.Add("npm start script configured");
            }
        }

        // Copilot understanding test
        if (AssessmentConfig.CopilotAvailable)
        {
            string response = CopilotService.AskCopilot("What is the exact command to run or start this application? Provide only the command.");
            if (!string.IsNullOrWhiteSpace(response) && !response.StartsWith("Error"))
            {
                int understanding = CopilotService.EvaluateCopilotUnderstanding(response, "run start execute");
                AssessmentConfig.Scores["Run"] += Math.Min(understanding, 4);
                if (understanding >= 3)
                    AssessmentConfig.Findings["Run"].Strengths.Add("Copilot understands how to run the application");
            }
        }
    }
}