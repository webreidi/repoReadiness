#!/usr/bin/env dotnet-script
/*
 * Repository Readiness Assessment Tool
 * Evaluates a repository's readiness for GitHub Copilot
 * 
 * Usage: dotnet script AssessRepo.cs -- <RepoPath> [--verbose]
 * Or compile: csc AssessRepo.cs && AssessRepo.exe <RepoPath>
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RepoReadiness
{
    class Program
    {
        static bool VerboseMode = false;
        static bool CopilotAvailable = false;
        
        // Scores
        static Dictionary<string, int> Scores = new Dictionary<string, int>
        {
            { "Build", 0 },
            { "Run", 0 },
            { "Test", 0 },
            { "Understanding", 0 },
            { "Documentation", 0 },
            { "CustomInstructions", 0 },
            { "CustomAgents", 0 },
            { "AgentSkills", 0 },
            { "Bonus", 0 }
        };
        
        // Findings
        static Dictionary<string, CategoryFindings> Findings = new Dictionary<string, CategoryFindings>
        {
            { "Build", new CategoryFindings() },
            { "Run", new CategoryFindings() },
            { "Test", new CategoryFindings() },
            { "Understanding", new CategoryFindings() },
            { "Documentation", new CategoryFindings() },
            { "CustomInstructions", new CategoryFindings() },
            { "CustomAgents", new CategoryFindings() },
            { "AgentSkills", new CategoryFindings() },
            { "CopilotReadiness", new CategoryFindings() }
        };
        
        // Copilot responses
        static string BuildCommand = "";
        static string RunCommand = "";
        static string TestCommand = "";
        
        static string originalDir = "";
        
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: AssessRepo <RepoPath> [--verbose]");
                Environment.Exit(1);
            }
            
            string repoPath = args[0];
            VerboseMode = args.Contains("--verbose") || args.Contains("-v");
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine("Repository Readiness Assessment");
            Console.WriteLine("Using GitHub Copilot CLI");
            Console.WriteLine("========================================");
            Console.ResetColor();
            Console.WriteLine();
            
            if (!Directory.Exists(repoPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Repository path not found: {repoPath}");
                Console.ResetColor();
                Environment.Exit(1);
            }
            
            CheckCopilotAvailability();
            
            // Save the directory where the tool is being run from (where AssessRepo.cs is)
            originalDir = Directory.GetCurrentDirectory();
            
            Directory.SetCurrentDirectory(repoPath);
            string repoName = Path.GetFileName(Path.GetFullPath(repoPath));
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Analyzing repository: {repoName}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Location: {repoPath}");
            Console.ResetColor();
            Console.WriteLine();
            
            // Run assessments
            AssessBuildCapability();
            AssessRunCapability();
            AssessTestCapability();
            AssessCodeUnderstanding();
            AssessDocumentation();
            
            // New Copilot-specific assessments
            AssessCustomInstructions();
            AssessCustomAgents();
            AssessAgentSkills();
            
            // Calculate Copilot readiness
            int copilotScore = CalculateCopilotReadiness();
            
            // Calculate final score and grade
            int totalScore = Scores.Values.Sum() + copilotScore;
            string grade = CalculateGrade(totalScore);
            
            DisplayResults(totalScore, grade, copilotScore);
            
            // Generate report
            string outputPath = GenerateReport(repoPath, repoName, totalScore, grade, copilotScore);
            
            Directory.SetCurrentDirectory(originalDir);
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Report saved to: {outputPath}");
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Assessment complete!");
            Console.ResetColor();
        }
        
        static void CheckCopilotAvailability()
        {
            try
            {
                var result = ExecuteCommand("copilot", "--version", captureOutput: true);
                if (result.ExitCode == 0)
                {
                    CopilotAvailable = true;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"GitHub Copilot CLI detected: {result.Output.Trim()}");
                    Console.ResetColor();
                    Console.WriteLine();
                }
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("GitHub Copilot CLI not found. Copilot CLI integration disabled.");
                Console.WriteLine("To enable Copilot CLI features:");
                Console.WriteLine("  1. Install Copilot CLI: https://docs.github.com/copilot");
                Console.WriteLine("  2. Authenticate with GitHub");
                Console.WriteLine("Proceeding with static analysis only...");
                Console.ResetColor();
                Console.WriteLine();
            }
        }
        
        static void AssessBuildCapability()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[1/8] Assessing Build Capability...");
            Console.ResetColor();
            
            string[] buildFiles = {
                "package.json", "package-lock.json", "yarn.lock", "pnpm-lock.yaml",
                "pom.xml", "build.gradle", "build.gradle.kts", "settings.gradle",
                "Makefile", "CMakeLists.txt", "Cargo.toml",
                "requirements.txt", "setup.py", "pyproject.toml", "Pipfile",
                "go.mod", "*.csproj", "*.sln", "*.fsproj",
                "composer.json", "Gemfile", "mix.exs", "rebar.config"
            };
            
            var foundBuildFiles = buildFiles.Where(f => 
                f.Contains("*") ? Directory.GetFiles(".", f).Length > 0 : File.Exists(f)
            ).ToList();
            
            if (foundBuildFiles.Any())
            {
                Scores["Build"] += 5;
                Findings["Build"].Strengths.Add($"Build configuration files found: {string.Join(", ", foundBuildFiles)}");
                
                // Ask Copilot how to build
                if (CopilotAvailable)
                {
                    Console.Write("  Asking Copilot how to build this project...");
                    Console.WriteLine();
                    BuildCommand = AskCopilot("Based on the files in this repository, what is the exact command to install dependencies and build this project? Provide only the command, no explanation.");
                    
                    if (!string.IsNullOrWhiteSpace(BuildCommand))
                    {
                        Findings["Build"].Details = $"Copilot Build Suggestion:\n{BuildCommand}\n\n";
                        
                        string[] validCommands = { "npm", "yarn", "pnpm", "mvn", "gradle", "dotnet", "cargo", "go build", "make", "pip", "poetry", "bundle", "composer", "powershell", "pwsh", "\\.ps1" };
                        bool understood = validCommands.Any(cmd => Regex.IsMatch(BuildCommand, cmd));
                        
                        if (understood)
                        {
                            Scores["Build"] += 10;
                            Findings["Build"].Strengths.Add("Copilot successfully understood build approach and provided actionable commands");
                        }
                        else
                        {
                            Scores["Build"] += 3;
                            Findings["Build"].Weaknesses.Add("Copilot provided response but couldn't clearly identify build commands");
                            Findings["Build"].Recommendations.Add("Add clearer build configuration or improve documentation");
                        }
                    }
                    else
                    {
                        Findings["Build"].Weaknesses.Add("Copilot could not determine how to build this project");
                        Findings["Build"].Recommendations.Add("Add clear build configuration files and documentation");
                    }
                }
            }
            else
            {
                Findings["Build"].Weaknesses.Add("No build configuration files detected");
                Findings["Build"].Recommendations.Add("Add appropriate build configuration for your language/framework");
                
                if (CopilotAvailable)
                {
                    Findings["Build"].Weaknesses.Add("Copilot cannot assist without build configuration files");
                }
            }
            
            // Check README
            if (File.Exists("README.md"))
            {
                string readme = File.ReadAllText("README.md");
                if (Regex.IsMatch(readme, @"(build|install|setup|getting started)", RegexOptions.IgnoreCase))
                {
                    Scores["Build"] += 5;
                    Findings["Build"].Strengths.Add("README contains build/setup instructions");
                }
                else
                {
                    Findings["Build"].Weaknesses.Add("README lacks build instructions");
                    Findings["Build"].Recommendations.Add("Add build/installation instructions to README");
                }
            }
            
            // Check for CI/CD
            bool hasCI = Directory.Exists(".github/workflows") ||
                        File.Exists(".gitlab-ci.yml") ||
                        File.Exists("azure-pipelines.yml") ||
                        File.Exists("Jenkinsfile");
            
            if (hasCI)
            {
                Scores["Build"] += 5;
                Scores["Bonus"] += 5;
                Findings["Build"].Strengths.Add("CI/CD configuration present");
            }
            else
            {
                Findings["Build"].Weaknesses.Add("No CI/CD configuration found");
                Findings["Build"].Recommendations.Add("Add CI/CD pipeline (GitHub Actions, GitLab CI, etc.)");
            }
        }
        
        static void AssessRunCapability()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[2/8] Assessing Run Capability...");
            Console.ResetColor();
            
            // Ask Copilot
            if (CopilotAvailable)
            {
                Console.Write("  Asking Copilot how to run this project...");
                Console.WriteLine();
                RunCommand = AskCopilot("Based on this repository, what is the exact command to run/start this application? Provide only the command, no explanation.");
                
                if (!string.IsNullOrWhiteSpace(RunCommand))
                {
                    Findings["Run"].Details = $"Copilot Run Suggestion:\n{RunCommand}\n\n";
                    
                    string[] validCommands = { "npm start", "npm run", "yarn start", "python", "java -jar", "dotnet run", "cargo run", "go run", "node", "ruby", "\\.ps1", "\\.sh", "\\.bat" };
                    bool understood = validCommands.Any(cmd => Regex.IsMatch(RunCommand, cmd));
                    
                    if (understood)
                    {
                        Scores["Run"] += 8;
                        Findings["Run"].Strengths.Add("Copilot successfully identified how to run the application");
                    }
                    else
                    {
                        Scores["Run"] += 2;
                        Findings["Run"].Weaknesses.Add("Copilot could not clearly identify run command");
                        Findings["Run"].Recommendations.Add("Add clear start scripts or document the application entry point");
                    }
                }
                else
                {
                    Findings["Run"].Weaknesses.Add("Copilot could not determine how to run this project");
                    Findings["Run"].Recommendations.Add("Add clear runtime configuration and entry point documentation");
                }
            }
            
            // Check for entry points
            string[] entryPoints = { "main.py", "app.py", "index.js", "server.js", "main.go", "Program.cs", "Main.java" };
            bool foundEntry = entryPoints.Any(File.Exists);
            
            if (foundEntry)
            {
                Scores["Run"] += 4;
                Findings["Run"].Strengths.Add($"Entry point identified: {entryPoints.First(File.Exists)}");
                
                if (CopilotAvailable)
                {
                    Scores["Run"] += 3;
                    Findings["Run"].Strengths.Add("Clear entry point exists, enabling Copilot to assist");
                }
            }
            else
            {
                Findings["Run"].Weaknesses.Add("No clear entry point identified");
                Findings["Run"].Recommendations.Add("Clearly document the main entry point for the application");
                
                if (CopilotAvailable)
                {
                    Findings["Run"].Weaknesses.Add("Without clear entry point, Copilot cannot reliably assist");
                }
            }
            
            // Check for environment config
            if (File.Exists(".env.example") || File.Exists(".env.sample") || File.Exists("env.example"))
            {
                Scores["Run"] += 5;
                Findings["Run"].Strengths.Add("Environment variable template provided");
            }
            else
            {
                Findings["Run"].Weaknesses.Add("No environment variable template found");
                Findings["Run"].Recommendations.Add("Add .env.example file with documented environment variables");
            }
        }
        
        static void AssessTestCapability()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[3/8] Assessing Test Capability...");
            Console.ResetColor();
            
            // Ask Copilot
            if (CopilotAvailable)
            {
                Console.Write("  Asking Copilot how to test this project...");
                Console.WriteLine();
                TestCommand = AskCopilot("Based on this repository, what is the exact command to run the test suite? Provide only the command, no explanation.");
                
                if (!string.IsNullOrWhiteSpace(TestCommand))
                {
                    Findings["Test"].Details = $"Copilot Test Suggestion:\n{TestCommand}\n\n";
                    
                    string[] validCommands = { "npm test", "yarn test", "pytest", "mvn test", "gradle test", "dotnet test", "cargo test", "go test" };
                    bool understood = validCommands.Any(cmd => Regex.IsMatch(TestCommand, cmd));
                    
                    if (understood)
                    {
                        Scores["Test"] += 8;
                        Findings["Test"].Strengths.Add("Copilot successfully identified how to run tests");
                    }
                    else
                    {
                        Scores["Test"] += 2;
                        Findings["Test"].Weaknesses.Add("Copilot could not clearly identify test execution command");
                        Findings["Test"].Recommendations.Add("Add clear test scripts and documentation");
                    }
                }
                else
                {
                    Findings["Test"].Weaknesses.Add("Copilot could not determine how to run tests");
                    Findings["Test"].Recommendations.Add("Add test framework configuration and documentation");
                }
            }
            
            // Check for test files
            string[] testPatterns = { "*.test.js", "*.spec.js", "*.test.ts", "*_test.go", "*_test.py", "*Test.java", "*Tests.cs" };
            var testFiles = testPatterns.SelectMany(p => Directory.GetFiles(".", p, SearchOption.AllDirectories)).ToList();
            
            if (testFiles.Any())
            {
                Scores["Test"] += 5;
                Findings["Test"].Strengths.Add($"Test files found: {testFiles.Count} test file(s)");
                
                if (CopilotAvailable && !string.IsNullOrWhiteSpace(TestCommand))
                {
                    Scores["Test"] += 3;
                    Findings["Test"].Strengths.Add("Test infrastructure exists and Copilot can assist");
                }
            }
            else
            {
                Findings["Test"].Weaknesses.Add("No test files detected");
                Findings["Test"].Recommendations.Add("Add test files following naming conventions");
                
                if (CopilotAvailable)
                {
                    Findings["Test"].Weaknesses.Add("Without test files, Copilot cannot assist with TDD");
                }
            }
        }
        
        static void AssessCodeUnderstanding()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[4/8] Assessing Code Understanding...");
            Console.ResetColor();
            
            // Check directory structure
            var topLevelDirs = Directory.GetDirectories(".").Select(Path.GetFileName).Where(d => !d.StartsWith(".")).ToList();
            if (topLevelDirs.Any(d => d == "src" || d == "lib" || d == "app" || d == "core"))
            {
                Scores["Understanding"] += 5;
                Findings["Understanding"].Strengths.Add("Clean top-level directory structure");
            }
            else
            {
                Findings["Understanding"].Weaknesses.Add("Unclear directory organization");
                Findings["Understanding"].Recommendations.Add("Organize code into clear directories (src, lib, tests, etc.)");
            }
            
            // Check for large files
            var codeFiles = Directory.GetFiles(".", "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".cs") || f.EndsWith(".js") || f.EndsWith(".py") || f.EndsWith(".java"))
                .Take(50);
            
            int largeFiles = codeFiles.Count(f => new FileInfo(f).Length > 1000000);
            if (largeFiles == 0)
            {
                Scores["Understanding"] += 5;
                Findings["Understanding"].Strengths.Add("No excessively large code files detected");
            }
            else
            {
                Findings["Understanding"].Weaknesses.Add($"{largeFiles} excessively large file(s) detected");
                Findings["Understanding"].Recommendations.Add("Break down large files into smaller, focused modules");
            }
        }
        
        static void AssessDocumentation()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[5/8] Assessing Documentation Quality...");
            Console.ResetColor();
            
            if (File.Exists("README.md"))
            {
                var readme = File.ReadAllText("README.md");
                int length = readme.Length;
                
                if (length > 5000)
                {
                    Scores["Documentation"] += 10;
                    Findings["Documentation"].Strengths.Add($"Comprehensive README.md present ({length} characters)");
                }
                else if (length > 1000)
                {
                    Scores["Documentation"] += 5;
                    Findings["Documentation"].Strengths.Add($"README.md present ({length} characters)");
                }
                else
                {
                    Scores["Documentation"] += 2;
                    Findings["Documentation"].Weaknesses.Add("README.md is too brief");
                }
            }
            else
            {
                Findings["Documentation"].Weaknesses.Add("No README.md found");
                Findings["Documentation"].Recommendations.Add("Create a README.md with project overview");
            }
        }
        
        static void AssessCustomInstructions()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[6/8] Assessing Custom Instructions Quality...");
            Console.ResetColor();
            
            var instructionFiles = new List<string>();
            
            if (File.Exists(".github/copilot-instructions.md"))
                instructionFiles.Add(".github/copilot-instructions.md");
            
            if (Directory.Exists(".github/instructions"))
            {
                var targetedInstructions = Directory.GetFiles(".github/instructions", "*.instructions.md", SearchOption.AllDirectories);
                instructionFiles.AddRange(targetedInstructions);
            }
            
            if (instructionFiles.Count == 0)
            {
                Findings["CustomInstructions"].Weaknesses.Add("No custom instruction files found");
                Findings["CustomInstructions"].Recommendations.Add("Create .github/copilot-instructions.md with project overview, tech stack, and coding standards");
                return;
            }
            
            Scores["CustomInstructions"] += 3;
            Findings["CustomInstructions"].Strengths.Add($"Found {instructionFiles.Count} instruction file(s)");
            
            // Analyze content depth
            var allContent = new StringBuilder();
            foreach (var file in instructionFiles)
            {
                allContent.AppendLine(File.ReadAllText(file));
            }
            
            string content = allContent.ToString();
            int contentLength = content.Length;
            
            if (contentLength < 200)
            {
                Scores["CustomInstructions"] += 2;
                Findings["CustomInstructions"].Weaknesses.Add("Instruction files are too brief to provide meaningful guidance");
                Findings["CustomInstructions"].Recommendations.Add("Add detailed project overview, tech stack, coding standards, and security practices");
                return;
            }
            
            // Check for key sections
            int sectionsFound = 0;
            var expectedSections = new Dictionary<string, string>
            {
                { "Project/Overview", @"(?i)(project|overview|purpose|application)" },
                { "Tech Stack/Technologies", @"(?i)(tech\s*stack|technolog|framework|language)" },
                { "Coding Standards", @"(?i)(coding\s*standard|convention|style|guideline)" },
                { "Security", @"(?i)(security|authentication|validation|password)" },
                { "Testing", @"(?i)(test|coverage|jest|pytest|junit)" }
            };
            
            foreach (var section in expectedSections)
            {
                if (Regex.IsMatch(content, section.Value))
                {
                    sectionsFound++;
                }
            }
            
            Scores["CustomInstructions"] += Math.Min(5, sectionsFound);
            
            if (sectionsFound >= 4)
                Findings["CustomInstructions"].Strengths.Add($"Comprehensive instructions with {sectionsFound}/5 key sections");
            else if (sectionsFound >= 2)
                Findings["CustomInstructions"].Strengths.Add($"Basic instructions present ({sectionsFound}/5 key sections)");
            else
                Findings["CustomInstructions"].Weaknesses.Add($"Instructions lack detail ({sectionsFound}/5 key sections found)");
            
            // Test Copilot understanding
            if (CopilotAvailable)
            {
                Console.Write("  Asking Copilot to interpret custom instructions...");
                Console.WriteLine();
                
                string copilotResponse = AskCopilot(@"Based on the custom instructions in this repository, answer:
1. What are the 3 most important coding standards?
2. What is the project's primary technology stack?
3. What security practices must be followed?
Provide specific answers based on the instructions.");
                
                if (!string.IsNullOrWhiteSpace(copilotResponse))
                {
                    Findings["CustomInstructions"].Details = $"Copilot Understanding Test:\n{copilotResponse}\n";
                    
                    // Score based on response quality
                    int responseScore = EvaluateCopilotUnderstanding(copilotResponse, content);
                    Scores["CustomInstructions"] += responseScore;
                    
                    if (responseScore >= 5)
                    {
                        Findings["CustomInstructions"].Strengths.Add("Copilot demonstrates excellent understanding of instructions");
                    }
                    else if (responseScore >= 3)
                    {
                        Findings["CustomInstructions"].Strengths.Add("Copilot shows partial understanding of instructions");
                        Findings["CustomInstructions"].Recommendations.Add("Make instructions more specific and actionable");
                    }
                    else
                    {
                        Findings["CustomInstructions"].Weaknesses.Add("Copilot struggles to extract meaningful guidance from instructions");
                        Findings["CustomInstructions"].Recommendations.Add("Restructure instructions with clear headings and specific requirements");
                    }
                }
            }
        }
        
        static void AssessCustomAgents()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[7/8] Assessing Custom Agents Quality...");
            Console.ResetColor();
            
            var agentFiles = new List<string>();
            
            if (Directory.Exists(".github/agents"))
            {
                agentFiles.AddRange(Directory.GetFiles(".github/agents", "*.md", SearchOption.TopDirectoryOnly));
            }
            
            if (agentFiles.Count == 0)
            {
                Findings["CustomAgents"].Weaknesses.Add("No custom agents found");
                Findings["CustomAgents"].Recommendations.Add("Consider creating specialized agents in .github/agents/ for common workflows");
                return;
            }
            
            Scores["CustomAgents"] += 2;
            Findings["CustomAgents"].Strengths.Add($"Found {agentFiles.Count} custom agent(s)");
            
            int wellConfiguredAgents = 0;
            var agentDescriptions = new List<string>();
            
            foreach (var agentFile in agentFiles)
            {
                string content = File.ReadAllText(agentFile);
                
                // Check for YAML frontmatter
                bool hasYaml = Regex.IsMatch(content, @"^---\s*\n", RegexOptions.Multiline);
                bool hasName = Regex.IsMatch(content, @"(?i)name\s*:", RegexOptions.Multiline);
                bool hasDescription = Regex.IsMatch(content, @"(?i)description\s*:", RegexOptions.Multiline);
                bool hasInstructions = content.Length > 500;
                
                if (hasYaml && hasName && hasDescription && hasInstructions)
                {
                    wellConfiguredAgents++;
                    
                    // Extract description for Copilot test
                    var match = Regex.Match(content, @"description\s*:\s*['""]?([^'"">\n]+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        agentDescriptions.Add($"- {Path.GetFileNameWithoutExtension(agentFile)}: {match.Groups[1].Value}");
                    }
                }
            }
            
            Scores["CustomAgents"] += wellConfiguredAgents * 2;
            
            if (wellConfiguredAgents == agentFiles.Count)
                Findings["CustomAgents"].Strengths.Add("All agents are well-configured with proper YAML and instructions");
            else if (wellConfiguredAgents > 0)
                Findings["CustomAgents"].Weaknesses.Add($"Only {wellConfiguredAgents}/{agentFiles.Count} agents are properly configured");
            else
                Findings["CustomAgents"].Weaknesses.Add("Agent files lack proper YAML frontmatter or detailed instructions");
            
            // Test Copilot understanding
            if (CopilotAvailable && agentDescriptions.Any())
            {
                Console.Write("  Asking Copilot to identify custom agents...");
                Console.WriteLine();
                
                string copilotResponse = AskCopilot("What specialized agents are available in this repository and what is each agent designed to help with?");
                
                if (!string.IsNullOrWhiteSpace(copilotResponse))
                {
                    Findings["CustomAgents"].Details = $"Copilot Agent Recognition:\n{copilotResponse}\n";
                    
                    // Check if Copilot identified the agents
                    int identifiedAgents = agentDescriptions.Count(desc => 
                        copilotResponse.Contains(desc.Split(':')[0].Trim('-', ' '), StringComparison.OrdinalIgnoreCase));
                    
                    if (identifiedAgents >= agentFiles.Count * 0.7)
                    {
                        Scores["CustomAgents"] += 4;
                        Findings["CustomAgents"].Strengths.Add("Copilot correctly identifies and understands custom agents");
                    }
                    else if (identifiedAgents > 0)
                    {
                        Scores["CustomAgents"] += 2;
                        Findings["CustomAgents"].Weaknesses.Add($"Copilot only recognized {identifiedAgents}/{agentFiles.Count} agents");
                        Findings["CustomAgents"].Recommendations.Add("Improve agent descriptions and documentation");
                    }
                    else
                    {
                        Findings["CustomAgents"].Weaknesses.Add("Copilot could not identify custom agents");
                        Findings["CustomAgents"].Recommendations.Add("Add clearer agent names and descriptions in YAML frontmatter");
                    }
                }
            }
        }
        
        static void AssessAgentSkills()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[8/8] Assessing Agent Skills Quality...");
            Console.ResetColor();
            
            var skillDirs = new List<string>();
            
            if (Directory.Exists(".github/skills"))
            {
                skillDirs.AddRange(Directory.GetDirectories(".github/skills"));
            }
            
            if (skillDirs.Count == 0)
            {
                Findings["AgentSkills"].Weaknesses.Add("No agent skills found");
                Findings["AgentSkills"].Recommendations.Add("Consider creating reusable skills in .github/skills/ for common tasks");
                return;
            }
            
            Scores["AgentSkills"] += 2;
            Findings["AgentSkills"].Strengths.Add($"Found {skillDirs.Count} skill(s)");
            
            int wellConfiguredSkills = 0;
            var skillDescriptions = new List<string>();
            
            foreach (var skillDir in skillDirs)
            {
                string skillFile = Path.Combine(skillDir, "SKILL.md");
                if (!File.Exists(skillFile))
                    continue;
                
                string content = File.ReadAllText(skillFile);
                
                // Check for proper structure
                bool hasYaml = Regex.IsMatch(content, @"^---\s*\n", RegexOptions.Multiline);
                bool hasName = Regex.IsMatch(content, @"(?i)name\s*:", RegexOptions.Multiline);
                bool hasDescription = Regex.IsMatch(content, @"(?i)description\s*:", RegexOptions.Multiline);
                bool hasInstructions = content.Length > 300;
                
                if (hasYaml && hasName && hasDescription && hasInstructions)
                {
                    wellConfiguredSkills++;
                    
                    string skillName = Path.GetFileName(skillDir);
                    var match = Regex.Match(content, @"description\s*:\s*['""]?([^'"">\n]+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        skillDescriptions.Add($"- {skillName}: {match.Groups[1].Value}");
                    }
                }
            }
            
            Scores["AgentSkills"] += wellConfiguredSkills * 2;
            
            if (wellConfiguredSkills == skillDirs.Count)
                Findings["AgentSkills"].Strengths.Add("All skills are properly configured with SKILL.md files");
            else if (wellConfiguredSkills > 0)
                Findings["AgentSkills"].Weaknesses.Add($"Only {wellConfiguredSkills}/{skillDirs.Count} skills have proper SKILL.md files");
            else
                Findings["AgentSkills"].Weaknesses.Add("Skill directories lack proper SKILL.md files");
            
            // Test Copilot understanding
            if (CopilotAvailable && skillDescriptions.Any())
            {
                Console.Write("  Asking Copilot to identify agent skills...");
                Console.WriteLine();
                
                string copilotResponse = AskCopilot("What specialized skills are configured for this repository and when should each be used?");
                
                if (!string.IsNullOrWhiteSpace(copilotResponse))
                {
                    Findings["AgentSkills"].Details = $"Copilot Skills Recognition:\n{copilotResponse}\n";
                    
                    // Check if Copilot identified the skills
                    int identifiedSkills = skillDescriptions.Count(desc => 
                    {
                        string skillName = desc.Split(':')[0].Trim('-', ' ');
                        return copilotResponse.Contains(skillName, StringComparison.OrdinalIgnoreCase);
                    });
                    
                    if (identifiedSkills >= skillDirs.Count * 0.7)
                    {
                        Scores["AgentSkills"] += 4;
                        Findings["AgentSkills"].Strengths.Add("Copilot correctly identifies and understands agent skills");
                    }
                    else if (identifiedSkills > 0)
                    {
                        Scores["AgentSkills"] += 2;
                        Findings["AgentSkills"].Weaknesses.Add($"Copilot only recognized {identifiedSkills}/{skillDirs.Count} skills");
                        Findings["AgentSkills"].Recommendations.Add("Improve skill descriptions and use cases");
                    }
                    else
                    {
                        Findings["AgentSkills"].Weaknesses.Add("Copilot could not identify agent skills");
                        Findings["AgentSkills"].Recommendations.Add("Add clearer skill names and descriptions in SKILL.md files");
                    }
                }
            }
        }
        
        static int EvaluateCopilotUnderstanding(string response, string sourceContent)
        {
            if (string.IsNullOrWhiteSpace(response) || response.Length < 50)
                return 0;
            
            int score = 0;
            
            // Check if response contains specific information (not generic)
            if (Regex.IsMatch(response, @"\b\d+\b") || // Contains numbers/versions
                response.Split('\n').Length >= 5) // Multi-line detailed response
            {
                score += 2;
            }
            
            // Check if response mentions specific technologies from source
            var techKeywords = new[] { "typescript", "javascript", "python", "java", "react", "vue", "angular", 
                                      "node", "dotnet", "c#", "go", "rust", "docker", "kubernetes" };
            
            int techMentions = techKeywords.Count(tech => 
                sourceContent.Contains(tech, StringComparison.OrdinalIgnoreCase) && 
                response.Contains(tech, StringComparison.OrdinalIgnoreCase));
            
            score += Math.Min(3, techMentions);
            
            // Check if response has structured format (numbered lists, bullet points)
            if (Regex.IsMatch(response, @"(^|\n)\s*[\d\-\*][\.\)]\s*", RegexOptions.Multiline))
            {
                score += 2;
            }
            
            return Math.Min(7, score);
        }
        
        static int CalculateCopilotReadiness()
        {
            if (!CopilotAvailable)
            {
                Findings["CopilotReadiness"].Weaknesses.Add("GitHub Copilot CLI not available - content understanding tests skipped");
                Findings["CopilotReadiness"].Recommendations.Add("Install GitHub Copilot CLI to enable dynamic content understanding validation");
                return 0;
            }
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine("Evaluating Copilot Readiness");
            Console.WriteLine("========================================");
            Console.ResetColor();
            
            int score = 0;
            string[] validBuildCmds = { "npm", "yarn", "mvn", "gradle", "dotnet", "cargo", "make", "pip", "powershell", "\\.ps1" };
            string[] validRunCmds = { "npm start", "python", "dotnet run", "cargo run", "go run", "node", "\\.ps1" };
            string[] validTestCmds = { "npm test", "pytest", "mvn test", "gradle test", "dotnet test", "cargo test", "go test" };
            
            bool buildSuccess = !string.IsNullOrWhiteSpace(BuildCommand) && validBuildCmds.Any(c => Regex.IsMatch(BuildCommand, c));
            bool runSuccess = !string.IsNullOrWhiteSpace(RunCommand) && validRunCmds.Any(c => Regex.IsMatch(RunCommand, c));
            bool testSuccess = !string.IsNullOrWhiteSpace(TestCommand) && validTestCmds.Any(c => Regex.IsMatch(TestCommand, c));
            
            // Build Process Understanding
            if (buildSuccess)
            {
                score += 10;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ Copilot understood build process (+10)");
                Findings["CopilotReadiness"].Strengths.Add($"Copilot successfully identified build commands: {BuildCommand.Substring(0, Math.Min(100, BuildCommand.Length))}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  ✗ Copilot could not determine build process (0)");
                
                if (string.IsNullOrWhiteSpace(BuildCommand))
                {
                    Findings["CopilotReadiness"].Weaknesses.Add("Copilot provided no response for build process - missing build configuration or unclear structure");
                    Findings["CopilotReadiness"].Recommendations.Add("Add clear build files (package.json, pom.xml, Makefile, etc.) with explicit build scripts");
                }
                else
                {
                    Findings["CopilotReadiness"].Weaknesses.Add($"Copilot's build response was unclear or generic: {BuildCommand.Substring(0, Math.Min(100, BuildCommand.Length))}");
                    Findings["CopilotReadiness"].Recommendations.Add("Improve build documentation in README with specific commands and prerequisites");
                }
            }
            Console.ResetColor();
            
            // Run Process Understanding
            if (runSuccess)
            {
                score += 10;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ Copilot understood run process (+10)");
                Findings["CopilotReadiness"].Strengths.Add($"Copilot successfully identified how to run the application: {RunCommand.Substring(0, Math.Min(100, RunCommand.Length))}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  ✗ Copilot could not determine run process (0)");
                
                if (string.IsNullOrWhiteSpace(RunCommand))
                {
                    Findings["CopilotReadiness"].Weaknesses.Add("Copilot provided no response for run process - missing entry point or start script");
                    Findings["CopilotReadiness"].Recommendations.Add("Add clear entry point documentation and 'start' script in package.json or equivalent");
                }
                else
                {
                    Findings["CopilotReadiness"].Weaknesses.Add($"Copilot's run response was unclear: {RunCommand.Substring(0, Math.Min(100, RunCommand.Length))}");
                    Findings["CopilotReadiness"].Recommendations.Add("Document the main entry point and provide clear 'Getting Started' instructions in README");
                }
            }
            Console.ResetColor();
            
            // Test Process Understanding
            if (testSuccess)
            {
                score += 10;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ Copilot understood test process (+10)");
                Findings["CopilotReadiness"].Strengths.Add($"Copilot successfully identified test execution: {TestCommand.Substring(0, Math.Min(100, TestCommand.Length))}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  ✗ Copilot could not determine test process (0)");
                
                if (string.IsNullOrWhiteSpace(TestCommand))
                {
                    Findings["CopilotReadiness"].Weaknesses.Add("Copilot provided no response for test process - missing test framework or test configuration");
                    Findings["CopilotReadiness"].Recommendations.Add("Add test framework (Jest, pytest, JUnit, etc.) and document test execution in README");
                }
                else
                {
                    Findings["CopilotReadiness"].Weaknesses.Add($"Copilot's test response was unclear: {TestCommand.Substring(0, Math.Min(100, TestCommand.Length))}");
                    Findings["CopilotReadiness"].Recommendations.Add("Add clear test script to package.json and document testing approach in README or TESTING.md");
                }
            }
            Console.ResetColor();
            
            // Overall Assessment
            Console.WriteLine();
            var color = score >= 20 ? ConsoleColor.Green : score >= 10 ? ConsoleColor.Yellow : ConsoleColor.Red;
            Console.ForegroundColor = color;
            Console.WriteLine($"Copilot Readiness Score: {score}/30");
            Console.ResetColor();
            
            // Add summary finding
            if (score == 30)
            {
                Findings["CopilotReadiness"].Strengths.Add("Excellent: Copilot demonstrates complete understanding of build, run, and test workflows");
            }
            else if (score >= 20)
            {
                Findings["CopilotReadiness"].Strengths.Add("Good: Copilot understands most workflows - minor improvements recommended");
                Findings["CopilotReadiness"].Recommendations.Add("Address the areas where Copilot struggled to achieve full readiness");
            }
            else if (score >= 10)
            {
                Findings["CopilotReadiness"].Weaknesses.Add("Partial understanding: Copilot only grasped some workflows - repository structure needs clarification");
                Findings["CopilotReadiness"].Recommendations.Add("Improve documentation and configuration for areas where Copilot failed to understand");
            }
            else
            {
                Findings["CopilotReadiness"].Weaknesses.Add("Critical: Copilot cannot understand how to work with this repository");
                Findings["CopilotReadiness"].Recommendations.Add("Add fundamental build, run, and test configurations before expecting effective AI assistance");
            }
            
            // Add details about what was tested
            Findings["CopilotReadiness"].Details = $@"Copilot CLI Integration Test Results:

Build Process Query:
{(string.IsNullOrWhiteSpace(BuildCommand) ? "  No response received" : $"  Response: {BuildCommand.Substring(0, Math.Min(200, BuildCommand.Length))}{(BuildCommand.Length > 200 ? "..." : "")}")}
  Result: {(buildSuccess ? "✓ Success" : "✗ Failed")}

Run Process Query:
{(string.IsNullOrWhiteSpace(RunCommand) ? "  No response received" : $"  Response: {RunCommand.Substring(0, Math.Min(200, RunCommand.Length))}{(RunCommand.Length > 200 ? "..." : "")}")}
  Result: {(runSuccess ? "✓ Success" : "✗ Failed")}

Test Process Query:
{(string.IsNullOrWhiteSpace(TestCommand) ? "  No response received" : $"  Response: {TestCommand.Substring(0, Math.Min(200, TestCommand.Length))}{(TestCommand.Length > 200 ? "..." : "")}")}
  Result: {(testSuccess ? "✓ Success" : "✗ Failed")}

This demonstrates {(score == 30 ? "that Copilot can fully assist developers with this repository." : score >= 20 ? "good Copilot readiness with minor gaps." : score >= 10 ? "partial Copilot readiness - improvements needed." : "that Copilot will struggle to assist effectively without better structure.")}
";
            
            return score;
        }
        
        static string CalculateGrade(int score)
        {
            int maxScore = CopilotAvailable ? 165 : 135;
            if (score >= maxScore * 0.9) return "A";
            if (score >= maxScore * 0.8) return "B";
            if (score >= maxScore * 0.7) return "C";
            if (score >= maxScore * 0.6) return "D";
            return "F";
        }
        
        static void DisplayResults(int totalScore, string grade, int copilotScore)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine("Calculation Complete");
            Console.WriteLine("========================================");
            Console.ResetColor();
            Console.WriteLine();
            
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Build Capability:          {Scores["Build"]}/25");
            Console.WriteLine($"Run Capability:            {Scores["Run"]}/20");
            Console.WriteLine($"Test Capability:           {Scores["Test"]}/20");
            Console.WriteLine($"Code Understanding:        {Scores["Understanding"]}/20");
            Console.WriteLine($"Documentation Quality:     {Scores["Documentation"]}/15");
            Console.WriteLine($"Custom Instructions:       {Scores["CustomInstructions"]}/15");
            Console.WriteLine($"Custom Agents:             {Scores["CustomAgents"]}/10");
            Console.WriteLine($"Agent Skills:              {Scores["AgentSkills"]}/10");
            if (CopilotAvailable)
            {
                var color = copilotScore >= 20 ? ConsoleColor.Green : copilotScore >= 10 ? ConsoleColor.Yellow : ConsoleColor.Red;
                Console.ForegroundColor = color;
                Console.WriteLine($"Copilot Readiness:         {copilotScore}/30");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            Console.WriteLine($"Bonus Points:              +{Scores["Bonus"]}");
            Console.ResetColor();
            Console.WriteLine();
            
            int maxScore = CopilotAvailable ? 165 : 135;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Total Score: {totalScore}/{maxScore} (+{Scores["Bonus"]} bonus)");
            
            var gradeColor = grade switch
            {
                "A" => ConsoleColor.Green,
                "B" => ConsoleColor.Cyan,
                "C" => ConsoleColor.Yellow,
                "D" => ConsoleColor.DarkYellow,
                _ => ConsoleColor.Red
            };
            Console.ForegroundColor = gradeColor;
            Console.WriteLine($"Final Grade: {grade}");
            Console.ResetColor();
            Console.WriteLine();
        }
        
        static string GenerateReport(string repoPath, string repoName, int totalScore, string grade, int copilotScore)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Generating detailed report...");
            Console.ResetColor();
            
            var sb = new StringBuilder();
            int maxScore = CopilotAvailable ? 165 : 135;
            
            sb.AppendLine("# Repository Readiness Report");
            sb.AppendLine();
            sb.AppendLine($"**Repository:** {repoName}");
            sb.AppendLine($"**Path:** {repoPath}");
            sb.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**Overall Grade:** {grade} ({totalScore}/{maxScore})");
            sb.AppendLine($"**Bonus Points:** +{Scores["Bonus"]}");
            sb.AppendLine($"**Copilot CLI:** {(CopilotAvailable ? "Enabled - Used for evaluation" : "Not available - Static analysis only")}");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## Executive Summary");
            sb.AppendLine();
            
            string readiness = grade switch
            {
                "A" => "excellently prepared for Copilot usage with minimal improvements needed",
                "B" => "well-prepared for Copilot usage with some minor enhancements recommended",
                "C" => "moderately prepared but requires several improvements for optimal Copilot experience",
                "D" => "poorly prepared and needs significant work before Copilot can be fully effective",
                _ => "not ready for effective Copilot usage and requires major restructuring"
            };
            
            sb.AppendLine($"This repository has been assessed for GitHub Copilot readiness. The overall grade of **{grade}** indicates that the repository is {readiness}.");
            sb.AppendLine();
            
            // Add detailed sections for each category
            AddCategorySection(sb, "Build Capability", Scores["Build"], 25, Findings["Build"]);
            AddCategorySection(sb, "Run Capability", Scores["Run"], 20, Findings["Run"]);
            AddCategorySection(sb, "Test Capability", Scores["Test"], 20, Findings["Test"]);
            AddCategorySection(sb, "Code Understanding", Scores["Understanding"], 20, Findings["Understanding"]);
            AddCategorySection(sb, "Documentation Quality", Scores["Documentation"], 15, Findings["Documentation"]);
            AddCategorySection(sb, "Custom Instructions", Scores["CustomInstructions"], 15, Findings["CustomInstructions"]);
            AddCategorySection(sb, "Custom Agents", Scores["CustomAgents"], 10, Findings["CustomAgents"]);
            AddCategorySection(sb, "Agent Skills", Scores["AgentSkills"], 10, Findings["AgentSkills"]);
            AddCategorySection(sb, "Copilot Readiness", copilotScore, 30, Findings["CopilotReadiness"]);
            
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("**Report Generated by Repository Readiness Assessment Tool (C# Version)**");
            sb.AppendLine("**Version 2.0 - Enhanced with Content-Aware Analysis**");
            
            // Save report in the original directory where the tool was run from
            // (where AssessRepo.cs is located, NOT the repo being assessed)
            string reportsDir = Path.Combine(originalDir, "readiness-reports");
            Directory.CreateDirectory(reportsDir);
            
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string outputPath = Path.Combine(reportsDir, $"{repoName}-readiness-report_{timestamp}.md");
            File.WriteAllText(outputPath, sb.ToString());
            
            return outputPath;
        }
        
        static void AddCategorySection(StringBuilder sb, string title, int score, int maxScore, CategoryFindings findings)
        {
            sb.AppendLine($"### {title}: {score}/{maxScore}");
            sb.AppendLine();
            
            sb.AppendLine("**Strengths:**");
            if (findings.Strengths.Any())
                foreach (var s in findings.Strengths)
                    sb.AppendLine($"- {s}");
            else
                sb.AppendLine("- None identified");
            sb.AppendLine();
            
            sb.AppendLine("**Weaknesses:**");
            if (findings.Weaknesses.Any())
                foreach (var w in findings.Weaknesses)
                    sb.AppendLine($"- {w}");
            else
                sb.AppendLine("- None identified");
            sb.AppendLine();
            
            sb.AppendLine("**Recommendations:**");
            if (findings.Recommendations.Any())
                foreach (var r in findings.Recommendations)
                    sb.AppendLine($"- {r}");
            else
                sb.AppendLine("- No specific recommendations");
            sb.AppendLine();
            
            if (!string.IsNullOrWhiteSpace(findings.Details))
            {
                sb.AppendLine("**Copilot Analysis:**");
                sb.AppendLine(findings.Details);
            }
            
            sb.AppendLine("---");
            sb.AppendLine();
        }
        
        static string AskCopilot(string question)
        {
            if (!CopilotAvailable)
                return "";
            
            if (VerboseMode)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"VERBOSE: Asking Copilot: {question}");
                Console.ResetColor();
            }
            
            try
            {
                var result = ExecuteCommand("copilot", $"-p \"{question}\"", captureOutput: true, timeout: 30000);
                
                if (VerboseMode)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"VERBOSE: Response length: {result.Output.Length} characters");
                    Console.ResetColor();
                }
                
                // Clean the response to remove Copilot CLI metadata
                return CleanCopilotResponse(result.Output);
            }
            catch (Exception ex)
            {
                if (VerboseMode)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"VERBOSE: Exception: {ex.Message}");
                    Console.ResetColor();
                }
                return "";
            }
        }
        
        static string CleanCopilotResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return "";
            
            // Remove Copilot CLI metadata lines (usage statistics, billing info, etc.)
            var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var cleanedLines = new List<string>();
            
            bool skipLine = false;
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Skip Copilot CLI internal operations (● bullets showing reads/lists/globs)
                if (trimmedLine.StartsWith("●") || 
                    trimmedLine.StartsWith("└") ||
                    trimmedLine.StartsWith("├") ||
                    trimmedLine.StartsWith("│"))
                {
                    continue;
                }
                
                // Skip metadata lines
                if (trimmedLine.Contains("Total usage est:") || 
                    trimmedLine.Contains("API time spent:") || 
                    trimmedLine.Contains("Total session time:") ||
                    trimmedLine.Contains("Total code changes:") ||
                    trimmedLine.Contains("Breakdown by AI model:") ||
                    trimmedLine.Contains("Premium request") ||
                    trimmedLine.Contains("cached") ||
                    Regex.IsMatch(trimmedLine, @"^\s*(claude|gpt).*\d+k in,"))
                {
                    skipLine = true;
                    continue;
                }
                
                // Skip lines that are just numbers or tokens
                if (skipLine && (string.IsNullOrWhiteSpace(trimmedLine) || 
                    Regex.IsMatch(trimmedLine, @"^\d+\.\d+k")))
                {
                    continue;
                }
                
                // Real content resets skip flag
                if (trimmedLine.Length > 0)
                {
                    skipLine = false;
                    cleanedLines.Add(line);
                }
            }
            
            var result = string.Join("\n", cleanedLines).Trim();
            
            // Additional cleanup: remove any remaining metadata patterns
            result = Regex.Replace(result, @"Total usage est:.*?(?=\n|$)", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"API time spent:.*?(?=\n|$)", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"Total session time:.*?(?=\n|$)", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"Total code changes:.*?(?=\n|$)", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"Breakdown by AI model:.*?(?=\n|$)", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"claude-sonnet.*?(?=\n|$)", "", RegexOptions.IgnoreCase);
            
            // Remove excessive blank lines
            result = Regex.Replace(result, @"\n{3,}", "\n\n");
            
            return result.Trim();
        }
        
        static ProcessResult ExecuteCommand(string command, string arguments, bool captureOutput = false, int timeout = 10000)
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = captureOutput,
                RedirectStandardError = captureOutput,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(psi);
            string output = "";
            
            if (captureOutput)
            {
                output = process.StandardOutput.ReadToEnd();
                output += process.StandardError.ReadToEnd();
            }
            
            process.WaitForExit(timeout);
            
            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                Output = output
            };
        }
        
        class CategoryFindings
        {
            public List<string> Strengths { get; set; } = new List<string>();
            public List<string> Weaknesses { get; set; } = new List<string>();
            public List<string> Recommendations { get; set; } = new List<string>();
            public string Details { get; set; } = "";
        }
        
        struct ProcessResult
        {
            public int ExitCode { get; set; }
            public string Output { get; set; }
        }
    }
}
