# GitHub Copilot SDK Integration

## Overview

The Repository Readiness Assessment tool integrates with **GitHub Copilot SDK** (`GitHub.Copilot.SDK`) to perform actual evaluations of repository capabilities. As of v2.2, the tool uses the official .NET SDK instead of directly spawning CLI processes.

## Architecture

```
┌─────────────────────────────────────────────┐
│          RepoReadiness Tool                 │
│                                             │
│  ┌─────────────┐    ┌──────────────────┐   │
│  │  Assessors  │───▶│  CopilotService  │   │
│  └─────────────┘    └────────┬─────────┘   │
│                              │              │
│                   ┌──────────▼─────────┐   │
│                   │ GitHub.Copilot.SDK │   │
│                   └──────────┬─────────┘   │
└──────────────────────────────│──────────────┘
                               │ JSON-RPC
                    ┌──────────▼─────────┐
                    │   Copilot CLI      │
                    │  (Server Mode)     │
                    └────────────────────┘
```

## What Changed in v2.2

### SDK Migration
**Old Approach (v2.1 and earlier):**
```csharp
// Spawning CLI process for each question
var process = new Process { FileName = "copilot", Arguments = $"-p \"{question}\"" };
process.Start();
string output = process.StandardOutput.ReadToEnd();
```

**New Approach (v2.2+):**
```csharp
// Using the official SDK
using GitHub.Copilot.SDK;

// Single client instance managed per assessment
await using var client = new CopilotClient();
await client.StartAsync();

// Create session and send message
await using var session = await client.CreateSessionAsync(new SessionConfig());
await session.SendAsync(new MessageOptions { Prompt = question });
```

### 1. **CopilotService Rewrite**
- `InitializeAsync()` - Initializes the SDK client once
- `ShutdownAsync()` - Cleans up resources properly
- `AskCopilot()` / `AskCopilotAsync()` - Uses SDK sessions
- `EvaluateCopilotUnderstanding()` - Unchanged, evaluates response quality

### 2. **Build Capability Assessment**
- Asks Copilot: "What is the exact command to install dependencies and build this project?"
- Uses SDK session to get response
- Adds bonus points if Copilot successfully identifies the build approach

### 3. **Run Capability Assessment**
- Asks Copilot: "What is the exact command to run or start this application?"
- Evaluates if Copilot can understand the application entry point
- Awards additional points for accurate responses

### 4. **Test Capability Assessment**
- Asks Copilot: "What is the exact command to run the test suite?"
- Checks if Copilot can identify the test execution method
- Provides bonus points for correct test command identification

### 5. **Custom Instructions Assessment**
- Asks Copilot: "Based on the custom instructions, what are the 3 most important coding standards?"
- Tests whether Copilot actually understands the project-specific guidance

### 6. **Custom Agents Assessment**
- Asks Copilot: "What specialized agents are available in this repository?"
- Validates Copilot recognizes configured agents

### 7. **Agent Skills Assessment**
- Asks Copilot: "What specialized skills are configured?"
- Confirms Copilot identifies available skill modules

## Prerequisites

### Install Copilot CLI

The SDK requires the Copilot CLI to be installed. It communicates with the CLI running in server mode.

**Install standalone Copilot CLI:**
```powershell
# Visit: https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli
# Follow instructions for your platform
```

### Verify Installation
```powershell
copilot --version
```

### Authenticate
```powershell
# The first time you run copilot, it will prompt you to authenticate
copilot
```

## Benefits

1. **Better Resource Management**: Single client instance reused across all assessments
2. **Improved Reliability**: SDK handles connection lifecycle automatically
3. **Event-Based Responses**: Proper async/event handling for responses
4. **Future-Proof**: SDK follows Copilot CLI updates automatically
5. **Cleaner Code**: No manual process spawning or output parsing
6. **Graceful Degradation**: Still works without Copilot CLI (basic analysis only)

## Configuration

The SDK client is configured in `CopilotService.cs`:

```csharp
_client = new CopilotClient(new CopilotClientOptions
{
    Cwd = AssessmentConfig.RepoPath,  // Working directory
    AutoStart = true,                  // Auto-start CLI server
    LogLevel = VerboseMode ? "debug" : "error"
});
```

Sessions are configured to disable tools (for faster responses):

```csharp
await using var session = await _client.CreateSessionAsync(new SessionConfig
{
    InfiniteSessions = new InfiniteSessionConfig { Enabled = false },
    ExcludedTools = new[] { "*" }  // Disable all tools for simple Q&A
});
```

## Usage

Same as before:
```powershell
dotnet run -- "C:\path\to\repo"
dotnet run -- "C:\path\to\repo" --verbose
```

The tool automatically:
1. Initializes the Copilot SDK client
2. Uses SDK sessions for enhanced analysis
3. Cleans up the client when complete
4. Falls back to basic analysis if SDK unavailable

## Troubleshooting

### SDK Not Available
```
Checking Copilot SDK availability... Not available
  (Install GitHub Copilot CLI for enhanced assessment)
```
**Solution:** Install and authenticate the Copilot CLI.

### Connection Errors
If the SDK fails to connect, check:
1. Copilot CLI is in PATH: `copilot --version`
2. You're authenticated: `copilot` (interactive)
3. Network connectivity to GitHub services

### Verbose Mode
Use `--verbose` to see detailed SDK communication:
```powershell
dotnet run -- "." --verbose
```

## SDK Documentation

For more details on the Copilot SDK:
- [GitHub Copilot SDK Repository](https://github.com/github/copilot-sdk)
- [.NET SDK Documentation](https://github.com/github/copilot-sdk/blob/main/dotnet/README.md)
- [Getting Started Guide](https://github.com/github/copilot-sdk/blob/main/docs/getting-started.md)
