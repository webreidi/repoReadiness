using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RepoReadiness.Assessors;
using RepoReadiness.Configuration;
using RepoReadiness.Services;

namespace RepoReadiness;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     Repository Readiness Assessment Tool v3.1             ║");
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

        // Check Copilot availability (this also initializes the SDK client)
        Console.Write("Checking Copilot SDK availability... ");
        AssessmentConfig.CopilotAvailable = CopilotService.CheckAvailability();
        if (AssessmentConfig.CopilotAvailable)
        {
            Console.WriteLine("Available ✓");
            Console.WriteLine("  (Enhanced assessment with content understanding tests using Copilot SDK)");
        }
        else
        {
            Console.WriteLine("Not available");
            Console.WriteLine("  (Install GitHub Copilot CLI for enhanced assessment)");
        }
        Console.WriteLine();

        try
        {
            // Run assessors - 9 base categories + 2 bonus categories
            var assessors = new IAssessor[]
            {
                new BuildAssessor(),
                new RunAssessor(),
                new TestAssessor(),
                new CodeQualityAssessor(),
                new CodeComplexityAssessor(),
                new DocumentationAssessor(),
                new CustomInstructionsAssessor(),
                new TypeSafetyAssessor(),
                new ContextFriendlinessAssessor(),
                // Bonus assessors (don't affect base grade)
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
        finally
        {
            // Cleanup Copilot SDK client
            if (AssessmentConfig.CopilotAvailable)
            {
                await CopilotService.ShutdownAsync();
            }
        }
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
        Console.WriteLine("Scoring (v3.1):");
        Console.WriteLine("  Base Score:  175 points max (determines grade)");
        Console.WriteLine("  Bonus:       +10 points max (Custom Agents, Agent Skills)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run -- .");
        Console.WriteLine("  dotnet run -- C:\\Projects\\MyRepo --verbose");
        Console.WriteLine("  dotnet run -- ../other-project");
        Console.WriteLine();
    }
}