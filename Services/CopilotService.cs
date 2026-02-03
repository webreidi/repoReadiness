using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Copilot.SDK;
using RepoReadiness.Configuration;

namespace RepoReadiness.Services;

public static class CopilotService
{
    private static CopilotClient? _client;
    private static bool _isInitialized = false;
    private static readonly object _lock = new();

    /// <summary>
    /// Initializes the Copilot SDK client. Call this before any assessments.
    /// </summary>
    public static async Task InitializeAsync()
    {
        if (_isInitialized) return;

        lock (_lock)
        {
            if (_isInitialized) return;

            try
            {
                _client = new CopilotClient(new CopilotClientOptions
                {
                    Cwd = AssessmentConfig.RepoPath,
                    AutoStart = true,
                    LogLevel = AssessmentConfig.VerboseMode ? "debug" : "error"
                });
            }
            catch (Exception ex)
            {
                if (AssessmentConfig.VerboseMode)
                    Console.WriteLine($"  SDK client creation failed: {ex.Message}");
                _client = null;
            }
        }

        if (_client != null)
        {
            try
            {
                await _client.StartAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                string errorMsg = ex.Message;
                if (errorMsg.Contains("JSON-RPC") || errorMsg.Contains("Communication error"))
                {
                    Console.WriteLine();
                    Console.WriteLine("  ERROR: GitHub Copilot CLI is not installed or not responding.");
                    Console.WriteLine("  To install, run: npm install -g @githubnext/github-copilot-cli");
                    Console.WriteLine("  Then authenticate: copilot auth");
                    Console.WriteLine("  More info: https://docs.github.com/en/copilot/github-copilot-in-the-cli");
                }
                else if (AssessmentConfig.VerboseMode)
                {
                    Console.WriteLine($"  SDK client start failed: {errorMsg}");
                }
                _client = null;
                _isInitialized = false;
            }
        }
    }

    /// <summary>
    /// Shuts down the Copilot SDK client. Call this after assessments complete.
    /// </summary>
    public static async Task ShutdownAsync()
    {
        if (_client != null)
        {
            try
            {
                await _client.StopAsync();
            }
            catch
            {
                // Best effort cleanup
            }
            finally
            {
                await _client.DisposeAsync();
                _client = null;
                _isInitialized = false;
            }
        }
    }

    /// <summary>
    /// Checks if Copilot CLI and SDK are available.
    /// </summary>
    public static bool CheckAvailability()
    {
        try
        {
            // Try to initialize synchronously to check availability
            InitializeAsync().GetAwaiter().GetResult();
            return _isInitialized && _client != null;
        }
        catch (Exception ex)
        {
            string errorMsg = ex.Message;
            if (errorMsg.Contains("JSON-RPC") || errorMsg.Contains("Communication error"))
            {
                Console.WriteLine();
                Console.WriteLine("  ERROR: GitHub Copilot CLI is not installed or not responding.");
                Console.WriteLine("  To install, run: npm install -g @githubnext/github-copilot-cli");
                Console.WriteLine("  Then authenticate: copilot auth");
            }
            else if (AssessmentConfig.VerboseMode)
            {
                Console.WriteLine($"  SDK availability check failed: {errorMsg}");
            }
            return false;
        }
    }

    /// <summary>
    /// Asks Copilot a question and returns the response.
    /// </summary>
    public static string AskCopilot(string question)
    {
        return AskCopilotAsync(question).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asks Copilot a question asynchronously and returns the response.
    /// </summary>
    public static async Task<string> AskCopilotAsync(string question)
    {
        if (_client == null || !_isInitialized)
        {
            return "Error: Copilot client not initialized";
        }

        try
        {
            // Create a session for this question
            await using var session = await _client.CreateSessionAsync(new SessionConfig
            {
                // Use default model
                InfiniteSessions = new InfiniteSessionConfig { Enabled = false },
                // Disable tools to get faster, simpler responses
                ExcludedTools = new List<string> { "*" }
            });

            var responseBuilder = new StringBuilder();
            var done = new TaskCompletionSource();
            var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            // Subscribe to events
            var subscription = session.On(evt =>
            {
                switch (evt)
                {
                    case AssistantMessageEvent msg:
                        responseBuilder.Append(msg.Data.Content);
                        break;
                    case SessionIdleEvent:
                        done.TrySetResult();
                        break;
                    case SessionErrorEvent err:
                        done.TrySetException(new Exception(err.Data.Message));
                        break;
                }
            });

            // Send the question
            await session.SendAsync(new MessageOptions { Prompt = question });

            // Wait for completion or timeout
            using (timeout.Token.Register(() => done.TrySetCanceled()))
            {
                try
                {
                    await done.Task;
                }
                catch (OperationCanceledException)
                {
                    return "Error: Request timed out";
                }
            }

            subscription.Dispose();

            string response = responseBuilder.ToString();

            if (AssessmentConfig.VerboseMode && !string.IsNullOrEmpty(response))
            {
                Console.WriteLine($"  Copilot response: {response.Substring(0, Math.Min(100, response.Length))}...");
            }

            return response;
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Evaluates how well Copilot understood the content based on its response.
    /// </summary>
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