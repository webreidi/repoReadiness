using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RepoReadiness.Configuration;

namespace RepoReadiness.Assessors;

/// <summary>
/// Assesses code complexity metrics that directly impact AI's ability to understand
/// and reason about the codebase. Measures cyclomatic complexity, coupling, and dependencies.
/// </summary>
public class CodeComplexityAssessor : IAssessor
{
    public string CategoryName => "CodeComplexity";
    public int MaxScore => 25;

    public void Assess()
    {
        Console.WriteLine("[5/11] Assessing Code Complexity & Dependencies...");

        var codeFiles = GetCodeFiles();
        
        if (!codeFiles.Any())
        {
            AssessmentConfig.Findings["CodeComplexity"].Weaknesses.Add("No code files found to analyze");
            return;
        }

        // 1. Cyclomatic Complexity Analysis (8 points)
        AssessCyclomaticComplexity(codeFiles);

        // 2. File Coupling Analysis (6 points)
        AssessFileCoupling(codeFiles);

        // 3. Circular Dependency Detection (6 points)
        AssessCircularDependencies(codeFiles);

        // 4. Dependency Depth Analysis (5 points)
        AssessDependencyDepth(codeFiles);
    }

    private List<string> GetCodeFiles()
    {
        var codeExtensions = new[] { ".cs", ".js", ".ts", ".tsx", ".jsx", ".py", ".java", ".go", ".rs", ".cpp", ".c", ".h" };
        var codeFiles = new List<string>();

        foreach (var ext in codeExtensions)
        {
            try
            {
                var files = Directory.GetFiles(AssessmentConfig.RepoPath, $"*{ext}", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("node_modules") && 
                                !f.Contains(".git") && 
                                !f.Contains("bin") && 
                                !f.Contains("obj") &&
                                !f.Contains("dist") &&
                                !f.Contains("build") &&
                                !f.Contains(".min."));
                codeFiles.AddRange(files);
            }
            catch { }
        }

        return codeFiles;
    }

    private void AssessCyclomaticComplexity(List<string> codeFiles)
    {
        var complexityScores = new List<int>();
        int analyzedMethods = 0;

        // Sample up to 20 files for performance
        foreach (var file in codeFiles.Take(20))
        {
            try
            {
                var content = File.ReadAllText(file);
                var methods = ExtractMethods(content, Path.GetExtension(file));

                foreach (var method in methods)
                {
                    int complexity = CalculateCyclomaticComplexity(method);
                    complexityScores.Add(complexity);
                    analyzedMethods++;
                }
            }
            catch { }
        }

        if (complexityScores.Any())
        {
            double avgComplexity = complexityScores.Average();
            int maxComplexity = complexityScores.Max();

            if (avgComplexity < 5)
            {
                AssessmentConfig.Scores["CodeComplexity"] += 8;
                AssessmentConfig.Findings["CodeComplexity"].Strengths.Add($"Excellent: Average cyclomatic complexity is {avgComplexity:F1} (very simple)");
            }
            else if (avgComplexity < 10)
            {
                AssessmentConfig.Scores["CodeComplexity"] += 6;
                AssessmentConfig.Findings["CodeComplexity"].Strengths.Add($"Good: Average cyclomatic complexity is {avgComplexity:F1} (manageable)");
            }
            else if (avgComplexity < 15)
            {
                AssessmentConfig.Scores["CodeComplexity"] += 3;
                AssessmentConfig.Findings["CodeComplexity"].Weaknesses.Add($"Moderate complexity: Average cyclomatic complexity is {avgComplexity:F1}");
                AssessmentConfig.Findings["CodeComplexity"].Recommendations.Add("Consider refactoring complex methods to reduce cyclomatic complexity");
            }
            else
            {
                AssessmentConfig.Findings["CodeComplexity"].Weaknesses.Add($"High complexity: Average cyclomatic complexity is {avgComplexity:F1} (hard for AI)");
                AssessmentConfig.Findings["CodeComplexity"].Recommendations.Add("Reduce cyclomatic complexity - AI struggles with highly complex methods");
            }

            if (maxComplexity > 20)
            {
                AssessmentConfig.Findings["CodeComplexity"].Weaknesses.Add($"Some methods have very high complexity (max: {maxComplexity})");
            }
        }
    }

    private List<string> ExtractMethods(string content, string extension)
    {
        var methods = new List<string>();
        
        // Different patterns for different languages
        Regex methodPattern = extension switch
        {
            ".cs" => new Regex(@"(public|private|protected|internal)\s+(?:static\s+)?(?:async\s+)?\w+(?:<[\w,\s]+>)?\s+\w+\s*\([^)]*\)\s*\{", RegexOptions.Multiline),
            ".js" or ".ts" or ".jsx" or ".tsx" => new Regex(@"(function\s+\w+\s*\([^)]*\)\s*\{|(?:const|let|var)\s+\w+\s*=\s*(?:async\s*)?\([^)]*\)\s*=>\s*\{|\w+\s*\([^)]*\)\s*\{)", RegexOptions.Multiline),
            ".py" => new Regex(@"def\s+\w+\s*\([^)]*\)\s*:", RegexOptions.Multiline),
            ".java" => new Regex(@"(public|private|protected)\s+(?:static\s+)?(?:final\s+)?\w+(?:<[\w,\s]+>)?\s+\w+\s*\([^)]*\)\s*\{", RegexOptions.Multiline),
            ".go" => new Regex(@"func\s+(?:\([\w\s*]+\)\s+)?\w+\s*\([^)]*\)\s*(?:[\w,\s\[\]\*]+)?\s*\{", RegexOptions.Multiline),
            _ => new Regex(@"\w+\s*\([^)]*\)\s*\{", RegexOptions.Multiline)
        };

        var matches = methodPattern.Matches(content);
        foreach (Match match in matches)
        {
            // Extract method body (simplified brace matching)
            int braceCount = 1;
            int start = match.Index + match.Length;
            int end = start;
            
            for (int i = start; i < content.Length && braceCount > 0; i++)
            {
                if (content[i] == '{') braceCount++;
                if (content[i] == '}') braceCount--;
                end = i;
            }
            
            if (end > start && end < content.Length)
            {
                methods.Add(content.Substring(match.Index, end - match.Index + 1));
            }
        }

        return methods;
    }

    private int CalculateCyclomaticComplexity(string methodBody)
    {
        // Cyclomatic complexity = 1 + number of decision points
        int complexity = 1;

        // Count decision points: if, else if, while, for, foreach, case, catch, &&, ||, ?
        var patterns = new[]
        {
            @"\bif\s*\(",
            @"\belse\s+if\s*\(",
            @"\bwhile\s*\(",
            @"\bfor\s*\(",
            @"\bforeach\s*\(",
            @"\bcase\s+",
            @"\bcatch\s*\(",
            @"&&",
            @"\|\|",
            @"\?"
        };

        foreach (var pattern in patterns)
        {
            complexity += Regex.Matches(methodBody, pattern).Count;
        }

        return complexity;
    }

    private void AssessFileCoupling(List<string> codeFiles)
    {
        var couplingScores = new Dictionary<string, int>();

        foreach (var file in codeFiles.Take(30))
        {
            try
            {
                var content = File.ReadAllText(file);
                int dependencies = CountFileDependencies(content, Path.GetExtension(file));
                couplingScores[file] = dependencies;
            }
            catch { }
        }

        if (couplingScores.Any())
        {
            double avgCoupling = couplingScores.Values.Average();
            int maxCoupling = couplingScores.Values.Max();

            if (avgCoupling < 5)
            {
                AssessmentConfig.Scores["CodeComplexity"] += 6;
                AssessmentConfig.Findings["CodeComplexity"].Strengths.Add($"Low coupling: Average {avgCoupling:F1} dependencies per file");
            }
            else if (avgCoupling < 10)
            {
                AssessmentConfig.Scores["CodeComplexity"] += 4;
                AssessmentConfig.Findings["CodeComplexity"].Strengths.Add($"Moderate coupling: Average {avgCoupling:F1} dependencies per file");
            }
            else if (avgCoupling < 15)
            {
                AssessmentConfig.Scores["CodeComplexity"] += 2;
                AssessmentConfig.Findings["CodeComplexity"].Weaknesses.Add($"High coupling: Average {avgCoupling:F1} dependencies per file");
                AssessmentConfig.Findings["CodeComplexity"].Recommendations.Add("Reduce file coupling to improve AI context understanding");
            }
            else
            {
                AssessmentConfig.Findings["CodeComplexity"].Weaknesses.Add($"Very high coupling: Average {avgCoupling:F1} dependencies per file");
                AssessmentConfig.Findings["CodeComplexity"].Recommendations.Add("Refactor to reduce dependencies - exceeds AI context window capacity");
            }

            if (maxCoupling > 20)
            {
                AssessmentConfig.Findings["CodeComplexity"].Weaknesses.Add($"Some files have excessive dependencies (max: {maxCoupling})");
            }
        }
    }

    private int CountFileDependencies(string content, string extension)
    {
        int count = 0;

        // Count import/using statements
        var patterns = extension switch
        {
            ".cs" => new[] { @"^using\s+[\w.]+;", @"^using\s+static\s+[\w.]+;" },
            ".js" or ".ts" or ".jsx" or ".tsx" => new[] { @"^import\s+.*from\s+['""]", @"require\s*\(['""]" },
            ".py" => new[] { @"^import\s+[\w.]+", @"^from\s+[\w.]+\s+import" },
            ".java" => new[] { @"^import\s+[\w.]+;" },
            ".go" => new[] { @"^import\s+\(", @"^import\s+""" },
            _ => new[] { @"^import\s+", @"^#include\s*[<""]" }
        };

        foreach (var pattern in patterns)
        {
            count += Regex.Matches(content, pattern, RegexOptions.Multiline).Count;
        }

        return count;
    }

    private void AssessCircularDependencies(List<string> codeFiles)
    {
        var dependencyGraph = BuildDependencyGraph(codeFiles.Take(50).ToList());
        var circularDeps = DetectCircularDependencies(dependencyGraph);

        if (circularDeps.Count == 0)
        {
            AssessmentConfig.Scores["CodeComplexity"] += 6;
            AssessmentConfig.Findings["CodeComplexity"].Strengths.Add("No circular dependencies detected");
        }
        else if (circularDeps.Count <= 2)
        {
            AssessmentConfig.Scores["CodeComplexity"] += 3;
            AssessmentConfig.Findings["CodeComplexity"].Weaknesses.Add($"Found {circularDeps.Count} circular dependency cycle(s)");
            AssessmentConfig.Findings["CodeComplexity"].Recommendations.Add("Break circular dependencies to improve code clarity");
        }
        else
        {
            AssessmentConfig.Findings["CodeComplexity"].Weaknesses.Add($"Found {circularDeps.Count} circular dependency cycles (confuses AI)");
            AssessmentConfig.Findings["CodeComplexity"].Recommendations.Add("Significant refactoring needed - circular dependencies prevent clear reasoning");
        }
    }

    private Dictionary<string, List<string>> BuildDependencyGraph(List<string> codeFiles)
    {
        var graph = new Dictionary<string, List<string>>();

        foreach (var file in codeFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            graph[fileName] = new List<string>();

            try
            {
                var content = File.ReadAllText(file);
                var extension = Path.GetExtension(file);

                // Extract imported modules/files
                var imports = ExtractImports(content, extension);
                
                foreach (var import in imports)
                {
                    // Check if import refers to another file in the codebase
                    var referencedFile = codeFiles.FirstOrDefault(f => 
                        Path.GetFileNameWithoutExtension(f).Contains(import) || 
                        f.Contains(import.Replace(".", "/")));
                    
                    if (referencedFile != null)
                    {
                        graph[fileName].Add(Path.GetFileNameWithoutExtension(referencedFile));
                    }
                }
            }
            catch { }
        }

        return graph;
    }

    private List<string> ExtractImports(string content, string extension)
    {
        var imports = new List<string>();
        
        var patterns = extension switch
        {
            ".cs" => new[] { @"using\s+([\w.]+);" },
            ".js" or ".ts" or ".jsx" or ".tsx" => new[] { @"import.*from\s+['""]\.\.?/([\w/]+)['""]", @"require\(['""]\.\.?/([\w/]+)['""]\)" },
            ".py" => new[] { @"from\s+([\w.]+)\s+import", @"import\s+([\w.]+)" },
            ".java" => new[] { @"import\s+([\w.]+);" },
            _ => Array.Empty<string>()
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(content, pattern, RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    imports.Add(match.Groups[1].Value);
                }
            }
        }

        return imports;
    }

    private List<List<string>> DetectCircularDependencies(Dictionary<string, List<string>> graph)
    {
        var cycles = new List<List<string>>();
        var visited = new HashSet<string>();
        var recStack = new HashSet<string>();

        foreach (var node in graph.Keys)
        {
            if (!visited.Contains(node))
            {
                var path = new List<string>();
                FindCycles(node, graph, visited, recStack, path, cycles);
            }
        }

        return cycles;
    }

    private bool FindCycles(string node, Dictionary<string, List<string>> graph, 
        HashSet<string> visited, HashSet<string> recStack, List<string> path, List<List<string>> cycles)
    {
        visited.Add(node);
        recStack.Add(node);
        path.Add(node);

        if (graph.ContainsKey(node))
        {
            foreach (var neighbor in graph[node])
            {
                if (!visited.Contains(neighbor))
                {
                    if (FindCycles(neighbor, graph, visited, recStack, path, cycles))
                        return true;
                }
                else if (recStack.Contains(neighbor))
                {
                    // Found a cycle
                    var cycleStart = path.IndexOf(neighbor);
                    if (cycleStart >= 0)
                    {
                        var cycle = path.Skip(cycleStart).ToList();
                        cycles.Add(cycle);
                    }
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        recStack.Remove(node);
        return false;
    }

    private void AssessDependencyDepth(List<string> codeFiles)
    {
        var dependencyGraph = BuildDependencyGraph(codeFiles.Take(50).ToList());
        var depths = new List<int>();

        foreach (var node in dependencyGraph.Keys)
        {
            int maxDepth = CalculateMaxDepth(node, dependencyGraph, new HashSet<string>());
            depths.Add(maxDepth);
        }

        if (depths.Any())
        {
            int maxDepth = depths.Max();
            double avgDepth = depths.Average();

            if (maxDepth <= 3)
            {
                AssessmentConfig.Scores["CodeComplexity"] += 5;
                AssessmentConfig.Findings["CodeComplexity"].Strengths.Add($"Shallow dependency chains: Max depth {maxDepth} (easy to understand)");
            }
            else if (maxDepth <= 5)
            {
                AssessmentConfig.Scores["CodeComplexity"] += 3;
                AssessmentConfig.Findings["CodeComplexity"].Strengths.Add($"Moderate dependency depth: Max {maxDepth} hops");
            }
            else if (maxDepth <= 8)
            {
                AssessmentConfig.Scores["CodeComplexity"] += 1;
                AssessmentConfig.Findings["CodeComplexity"].Weaknesses.Add($"Deep dependency chains: Max depth {maxDepth}");
                AssessmentConfig.Findings["CodeComplexity"].Recommendations.Add("Flatten dependency chains for better AI comprehension");
            }
            else
            {
                AssessmentConfig.Findings["CodeComplexity"].Weaknesses.Add($"Very deep dependency chains: Max depth {maxDepth} (exceeds AI context)");
                AssessmentConfig.Findings["CodeComplexity"].Recommendations.Add("Critical: Dependency depth requires understanding too much context for AI");
            }
        }
    }

    private int CalculateMaxDepth(string node, Dictionary<string, List<string>> graph, HashSet<string> visited)
    {
        if (!graph.ContainsKey(node) || visited.Contains(node))
            return 0;

        visited.Add(node);
        int maxDepth = 0;

        foreach (var neighbor in graph[node])
        {
            int depth = CalculateMaxDepth(neighbor, graph, new HashSet<string>(visited));
            maxDepth = Math.Max(maxDepth, depth);
        }

        return maxDepth + 1;
    }
}
