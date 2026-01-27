using System;
using System.Diagnostics;
using System.Linq;
using RepoReadiness.Configuration;

namespace RepoReadiness.Services;

public static class CopilotService
{
    public static bool CheckAvailability()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "copilot",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static string AskCopilot(string question)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "copilot",
                    Arguments = $"-p \"{question}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = AssessmentConfig.RepoPath
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(30000);

            if (AssessmentConfig.VerboseMode)
                Console.WriteLine($"  Copilot response: {output.Substring(0, Math.Min(100, output.Length))}...");

            return output;
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public static int EvaluateCopilotUnderstanding(string response, string sourceContent)
    {
        if (string.IsNullOrWhiteSpace(response) || response.StartsWith("Error"))
            return 0;

        int score = 0;

        // Multi-line detailed response
        if (response.Split('\n').Length > 3)
            score += 2;

        // Tech-specific mentions from source
        var techTerms = new[] { "dotnet", "npm", "yarn", "maven", "gradle", "cargo", "pip", "pytest", "jest", "xunit", "nunit", "mstest" };
        int techMatches = 0;
        foreach (var term in techTerms)
        {
            if (sourceContent.Contains(term, StringComparison.OrdinalIgnoreCase) &&
                response.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                techMatches++;
                if (techMatches >= 3) break;
            }
        }
        score += techMatches;

        // Structured format (lists, bullets)
        if (response.Contains("- ") || response.Contains("* ") || response.Contains("1.") || response.Contains("```"))
            score += 2;

        return Math.Min(score, 7);
    }
}