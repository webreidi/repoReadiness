using System;
using System.IO;
using System.Linq;
using RepoReadiness.Assessors;
using RepoReadiness.Configuration;
using RepoReadiness.Services;

namespace RepoReadiness;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     Repository Readiness Assessment Tool v2.0             ║");
        Console.WriteLine("║     GitHub Copilot Optimization Analyzer                  ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Parse arguments
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            ShowUsage();
            return;
        }

        AssessmentConfig.RepoPath = Path.GetFullPath(args[0]);
        AssessmentConfig.VerboseMode = args.Contains("--verbose") || args.Contains("-v");

        if (!Directory.Exists(AssessmentConfig.RepoPath))
        {
            Console.WriteLine($"Error: Directory not found: {AssessmentConfig.RepoPath}");
            return;
        }

        Console.WriteLine($"Assessing: {AssessmentConfig.RepoPath}");
        Console.WriteLine();

        // Check Copilot availability
        Console.Write("Checking Copilot CLI availability... ");
        AssessmentConfig.CopilotAvailable = CopilotService.CheckAvailability();
        if (AssessmentConfig.CopilotAvailable)
        {
            Console.WriteLine("Available ✓");
            Console.WriteLine("  (Enhanced assessment with content understanding tests)");
        }
        else
        {
            Console.WriteLine("Not available");
            Console.WriteLine("  (Install GitHub Copilot CLI for enhanced assessment)");
        }
        Console.WriteLine();

        // Run assessors
        var assessors = new IAssessor[]
        {
            new BuildAssessor(),
            new RunAssessor(),
            new TestAssessor(),
            new CodeUnderstandingAssessor(),
            new DocumentationAssessor(),
            new CustomInstructionsAssessor(),
            new CustomAgentsAssessor(),
            new AgentSkillsAssessor()
        };

        foreach (var assessor in assessors)
        {
            try
            {
                assessor.Assess();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Warning: Error in {assessor.CategoryName}: {ex.Message}");
            }
        }

        // Display summary
        ReportGenerator.DisplaySummary();

        // Generate report
        var repoName = new DirectoryInfo(AssessmentConfig.RepoPath).Name;
        ReportGenerator.GenerateReport(repoName);

        Console.WriteLine("Assessment complete!");
    }

    static void ShowUsage()
    {
        Console.WriteLine("Usage: dotnet run -- <repository-path> [options]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <repository-path>    Path to the repository to assess");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --verbose, -v        Show detailed output during assessment");
        Console.WriteLine("  --help, -h           Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run -- .");
        Console.WriteLine("  dotnet run -- C:\\Projects\\MyRepo --verbose");
        Console.WriteLine("  dotnet run -- ../other-project");
        Console.WriteLine();
    }
}