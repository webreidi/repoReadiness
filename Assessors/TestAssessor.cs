using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RepoReadiness.Configuration;
using RepoReadiness.Services;

namespace RepoReadiness.Assessors;

public class TestAssessor : IAssessor
{
    public string CategoryName => "Test";
    public int MaxScore => 20;

    public void Assess()
    {
        Console.WriteLine("[3/8] Assessing Test Capability...");

        // Check for test frameworks
        var testIndicators = new Dictionary<string, string>
        {
            { "xunit", "*.csproj" },
            { "nunit", "*.csproj" },
            { "mstest", "*.csproj" },
            { "jest", "package.json" },
            { "mocha", "package.json" },
            { "pytest", "requirements.txt" },
            { "junit", "pom.xml" }
        };

        bool hasTestFramework = false;
        foreach (var indicator in testIndicators)
        {
            var files = Directory.GetFiles(AssessmentConfig.RepoPath, indicator.Value, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    if (content.Contains(indicator.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        hasTestFramework = true;
                        AssessmentConfig.Scores["Test"] += 5;
                        AssessmentConfig.Findings["Test"].Strengths.Add($"Test framework detected: {indicator.Key}");
                        break;
                    }
                }
                catch { }
            }
            if (hasTestFramework) break;
        }

        if (!hasTestFramework)
        {
            AssessmentConfig.Findings["Test"].Weaknesses.Add("No test framework detected");
            AssessmentConfig.Findings["Test"].Recommendations.Add("Add a test framework (xUnit, Jest, pytest, etc.)");
        }

        // Check for test files
        var testPatterns = new[] { "*Test*.cs", "*Tests*.cs", "*.test.js", "*.spec.js", "test_*.py", "*_test.py" };
        int testFileCount = 0;
        foreach (var pattern in testPatterns)
        {
            try
            {
                testFileCount += Directory.GetFiles(AssessmentConfig.RepoPath, pattern, SearchOption.AllDirectories).Length;
            }
            catch { }
        }

        if (testFileCount > 0)
        {
            AssessmentConfig.Scores["Test"] += 5;
            AssessmentConfig.Findings["Test"].Strengths.Add($"Found {testFileCount} test file(s)");
        }
        else
        {
            AssessmentConfig.Findings["Test"].Weaknesses.Add("No test files found");
            AssessmentConfig.Findings["Test"].Recommendations.Add("Add unit tests for your code");
        }

        // Check for test script in package.json
        var packageJson = Path.Combine(AssessmentConfig.RepoPath, "package.json");
        if (File.Exists(packageJson))
        {
            var content = File.ReadAllText(packageJson);
            if (content.Contains("\"test\""))
            {
                AssessmentConfig.Scores["Test"] += 5;
                AssessmentConfig.Findings["Test"].Strengths.Add("npm test script configured");
            }
        }

        // Check test organization (tests folder)
        var testDirs = new[] { "tests", "test", "Tests", "__tests__", "spec" };
        foreach (var dir in testDirs)
        {
            if (Directory.Exists(Path.Combine(AssessmentConfig.RepoPath, dir)))
            {
                AssessmentConfig.Scores["Test"] += 3;
                AssessmentConfig.Findings["Test"].Strengths.Add($"Organized test directory: {dir}/");
                break;
            }
        }

        // Copilot understanding test
        if (AssessmentConfig.CopilotAvailable)
        {
            string response = CopilotService.AskCopilot("What is the exact command to run the test suite? Provide only the command.");
            if (!string.IsNullOrWhiteSpace(response) && !response.StartsWith("Error"))
            {
                int understanding = CopilotService.EvaluateCopilotUnderstanding(response, "test");
                AssessmentConfig.Scores["Test"] += Math.Min(understanding, 2);
                if (understanding >= 2)
                    AssessmentConfig.Findings["Test"].Strengths.Add("Copilot understands test execution");
            }
        }
    }
}