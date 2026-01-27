# Repository Readiness Assessment Tool

## Project Overview

This is a code quality and GitHub Copilot readiness assessment tool for software repositories. It evaluates repositories across 8 dimensions to determine how effectively GitHub Copilot can assist developers. The tool performs both static analysis and dynamic testing using the GitHub Copilot CLI to verify that custom instructions, agents, and skills are actually understood by Copilot.

**Primary Users:** DevOps teams, engineering managers, and developers preparing repositories for AI-assisted development.

**Core Purpose:** Provide actionable insights into repository structure, documentation, and Copilot-specific configurations to maximize AI assistance effectiveness.

## Tech Stack

### Languages & Frameworks
- **Primary Language:** C# (.NET 6.0+)
- **Script Version:** PowerShell 5.1+ (legacy support in `assess-repo.ps1.old`)
- **Runtime:** .NET SDK 6.0 or higher required
- **Execution:** `dotnet run` or `dotnet script` for standalone execution

### Key Dependencies
- **System.IO**: File system operations
- **System.Text.RegularExpressions**: Pattern matching for content analysis
- **System.Diagnostics**: Process execution for Copilot CLI integration
- **No external NuGet packages**: Uses only standard .NET libraries

### External Tools Integration
- **GitHub Copilot CLI**: Optional but recommended for content understanding tests
- **Git**: Repository structure detection (not explicitly required)

### File Structure
```
repoReadiness/
├── AssessRepo.cs                    # Main assessment tool (C#)
├── assess-repo.ps1.old              # Legacy PowerShell version
├── README.md                        # User documentation
├── CHANGELOG.md                     # Version history
├── INSTRUCTIONS_GUIDE.md            # Guide for creating instructions
├── IMPLEMENTATION_SUMMARY.md        # Technical details
├── repo-readiness-agent.md          # Original assessment criteria
├── .github/
│   └── copilot-instructions.md      # This file
└── readiness-reports/               # Generated assessment reports
```

## Coding Standards

### Naming Conventions
- **Classes:** PascalCase (e.g., `Program`, `CategoryFindings`)
- **Methods:** PascalCase (e.g., `AssessCustomInstructions()`)
- **Variables:** camelCase (e.g., `repoPath`, `copilotAvailable`)
- **Constants:** PascalCase with descriptive names (e.g., `VerboseMode`)
- **Static fields:** PascalCase (e.g., `Scores`, `Findings`)

### Code Organization
- **Single file architecture**: All code in `AssessRepo.cs` (~1000 lines)
- **Static methods**: All methods are static within `Program` class
- **Method grouping**: Assessment methods grouped by category
- **Helper methods**: Placed after main assessment logic (e.g., `AskCopilot`, `EvaluateCopilotUnderstanding`)

### Required Patterns
- **Error handling**: Use `try-catch` for external process calls (Copilot CLI)
- **Null checking**: Check file existence before reading
- **StringBuilder**: Use for building large strings (reports)
- **LINQ**: Use for collection operations and filtering
- **String interpolation**: Prefer `$"{variable}"` over concatenation
- **Regex**: Use compiled or pre-defined patterns for performance

### Method Structure
Each assessment method follows this pattern:
```csharp
static void AssessCategory()
{
    // 1. Display progress
    Console.WriteLine("[X/8] Assessing Category...");
    
    // 2. Detect relevant files/configurations
    var files = Directory.GetFiles(...);
    
    // 3. Award points for presence
    if (files.Any())
        Scores["Category"] += points;
    
    // 4. Analyze content (if Copilot CLI available)
    if (CopilotAvailable)
    {
        string response = AskCopilot("question");
        // Evaluate and score response
    }
    
    // 5. Record findings
    Findings["Category"].Strengths.Add("...");
    Findings["Category"].Recommendations.Add("...");
}
```

### Forbidden Patterns
- ❌ Don't use `Console.ReadLine()` - tool must run non-interactively
- ❌ Avoid hardcoded paths - use relative paths and `Path.Combine()`
- ❌ Don't throw exceptions for missing files - gracefully handle and report
- ❌ No GUI dependencies - console-only tool
- ❌ Don't modify the repository being assessed

## Security Practices

### Input Validation
- **Repository paths**: Validate with `Directory.Exists()` before processing
- **File paths**: Use `Path.GetFullPath()` to resolve and validate
- **Command arguments**: Parse and validate before use
- **Process execution**: Sanitize inputs to prevent command injection

### Safe File Operations
- **Read-only access**: Never write to the repository being assessed (except reports directory)
- **Report output**: Write reports to dedicated `readiness-reports/` folder in tool directory
- **Temp files**: Clean up if created (currently none used)
- **Path traversal**: Use `Path.Combine()` and validate paths

### Process Security
- **Copilot CLI calls**: Execute with timeout to prevent hanging
- **Error output**: Capture and log stderr
- **Exit codes**: Check process exit codes before trusting output

### Data Protection
- **No credentials**: Tool doesn't handle API keys or secrets
- **No PII**: Doesn't collect or transmit user data
- **Local execution**: All processing happens locally

## Testing Requirements

### Current State
- **No formal test suite**: Tool is self-validating (assesses its own repo)
- **Manual testing**: Run against known good/bad repositories
- **Dogfooding**: Run `AssessRepo.cs` on `repoReadiness` directory

### Testing Approach
When making changes:

1. **Syntax validation**: Ensure `dotnet run AssessRepo.cs` compiles
2. **Self-assessment**: Run on tool's own directory
3. **Test repositories**: Create sample repos with/without:
   - Build configurations
   - Custom instructions
   - Custom agents
   - Agent skills
4. **Copilot CLI integration**: Verify CLI detection and responses
5. **Report validation**: Check generated markdown reports

### Test Cases to Consider
- Empty repository
- Repository without build config
- Repository with `.github/copilot-instructions.md`
- Repository with custom agents
- Repository with agent skills
- Large repository (performance testing)
- Repository without Copilot CLI installed

### Manual Test Commands
```bash
# Test compilation
dotnet run AssessRepo.cs

# Test on current directory
dotnet run AssessRepo.cs -- "."

# Test with verbose output
dotnet run AssessRepo.cs -- "." --verbose

# Test on external repository
dotnet run AssessRepo.cs -- "C:\path\to\test\repo"
```

## Assessment Categories & Scoring

### Scoring System (165 points maximum with Copilot CLI)

1. **Build Capability (25 points)**
   - Presence of build files: 5
   - README build instructions: 5
   - CI/CD configuration: 5
   - Build scripts: 5
   - No hardcoded paths: 5

2. **Run Capability (20 points)**
   - Entry point identified: 4
   - Environment config template: 5
   - Launch configurations: 3
   - Start script: 3
   - Copilot understanding: 5

3. **Test Capability (20 points)**
   - Test framework detected: 5
   - Test files found: 5
   - Test script configured: 5
   - Test organization: 3
   - Copilot understanding: 2

4. **Code Understanding (20 points)**
   - Clean directory structure: 4-5
   - No oversized files: 4-5
   - TypeScript/types: 4
   - Consistent naming: 4
   - Linting configured: 4

5. **Documentation Quality (15 points)**
   - README present: 2-10 (based on length)
   - Additional docs: 3-5
   - API docs: 3
   - Inline comments: 2

6. **Custom Instructions (15 points)**
   - File presence: 3
   - Content length: 2
   - Key sections (5): up to 5
   - **Copilot understanding**: up to 7

7. **Custom Agents (10 points)**
   - Agent files found: 2
   - Proper YAML config: 2 per agent
   - **Copilot recognition**: up to 4

8. **Agent Skills (10 points)**
   - Skill directories: 2
   - SKILL.md files: 2 per skill
   - **Copilot identification**: up to 4

9. **Copilot Readiness (30 points)**
   - Build understanding: 10
   - Run understanding: 10
   - Test understanding: 10

10. **Bonus Points (up to 15)**
    - GitHub Actions: +5
    - TypeScript: +2
    - EditorConfig: +2
    - Other best practices: +1-3 each

## Key Implementation Details

### Copilot CLI Integration
The tool uses `AskCopilot()` helper method to interact with Copilot CLI:

```csharp
static string AskCopilot(string question)
{
    // Execute: copilot -p "question"
    // Capture output
    // Return response text
}
```

**Questions asked:**
1. "What is the exact command to install dependencies and build?"
2. "What is the exact command to run/start this application?"
3. "What is the exact command to run the test suite?"
4. "Based on instructions, what are the 3 most important coding standards?"
5. "What specialized agents are available?"
6. "What specialized skills are configured?"

### Content Understanding Evaluation
The `EvaluateCopilotUnderstanding()` method scores responses 0-7 based on:
- Response depth (multi-line, detailed): +2
- Tech-specific mentions (from source): +0-3
- Structured format (lists, bullets): +2

### Report Generation
Reports are markdown files with:
- Repository metadata
- Overall grade and scores
- Category-by-category breakdown
- Strengths/weaknesses/recommendations per category
- Priority actions list
- Copilot understanding test results

## Common Tasks

### Adding a New Assessment Category

1. **Add to Scores dictionary** (lines 26-34):
```csharp
{ "NewCategory", 0 }
```

2. **Add to Findings dictionary** (lines 37-45):
```csharp
{ "NewCategory", new CategoryFindings() }
```

3. **Create assessment method**:
```csharp
static void AssessNewCategory()
{
    Console.WriteLine("[X/Y] Assessing New Category...");
    // Detection logic
    // Scoring logic
    // Copilot testing (if applicable)
    // Record findings
}
```

4. **Call from Main()** (after line 110):
```csharp
AssessNewCategory();
```

5. **Update report generation** (line 985+):
```csharp
AddCategorySection(sb, "New Category", Scores["NewCategory"], maxPoints, Findings["NewCategory"]);
```

6. **Update console display** (line 913):
```csharp
Console.WriteLine($"New Category:              {Scores["NewCategory"]}/{maxPoints}");
```

7. **Update max score** in `CalculateGrade()` (line 891)

### Modifying Copilot Questions

Find the relevant `AskCopilot()` call and update the question string:
```csharp
string response = AskCopilot("New question here?");
```

**Best practices for questions:**
- Be specific and focused
- Request exact commands/information
- Keep questions under 200 characters
- End with "Provide only X, no explanation" if needed

### Adjusting Scoring Thresholds

Locate the category method and modify score assignments:
```csharp
if (condition)
{
    Scores["Category"] += 5; // Change this value
}
```

Grade thresholds in `CalculateGrade()`:
```csharp
if (score >= maxScore * 0.9) return "A"; // 90%
```

## Environment Configuration

### Required Environment
- **.NET SDK 6.0+**: Must be installed and in PATH
- **Windows/macOS/Linux**: Cross-platform compatible
- **GitHub Copilot CLI**: Optional (tool works without it, but scores lower)

### No Configuration Files
The tool requires no configuration files - it's self-contained.

### Running the Tool

**Basic execution:**
```bash
dotnet run AssessRepo.cs -- "<repo-path>"
```

**With verbose output:**
```bash
dotnet run AssessRepo.cs -- "<repo-path>" --verbose
```

**On current directory:**
```bash
dotnet run AssessRepo.cs -- "."
```

### Output Location
Reports saved to: `<tool-directory>/readiness-reports/`

Format: `{repoName}-readiness-report_{timestamp}.md`

## Documentation Standards

### Code Comments
- **Use sparingly**: Code should be self-documenting through clear naming
- **Explain "why" not "what"**: Comment complex logic or non-obvious decisions
- **Document regex patterns**: Explain what pattern matches
- **Header comments**: None required for methods (names are descriptive)

### Inline Documentation Examples

**Good:**
```csharp
// Check if Copilot understands by asking it to extract guidance
string response = AskCopilot("Based on instructions...");

// Score 0-7 based on response quality and specificity
int score = EvaluateCopilotUnderstanding(response, content);
```

**Unnecessary:**
```csharp
// Add 5 points
Scores["Build"] += 5;

// Check if file exists
if (File.Exists("README.md"))
```

### Markdown Documentation
All supporting documentation uses markdown:
- **Headers**: Use proper hierarchy (# ## ###)
- **Code blocks**: Specify language for syntax highlighting
- **Lists**: Use bullets or numbers consistently
- **Links**: Use relative paths for internal docs

## Version History

- **v2.0** (2026-01-27): Content-aware assessment with Copilot understanding tests
- **v1.0** (2026-01-26): Initial release with 5 core categories

## Related Resources

- [README.md](../README.md) - User guide and usage instructions
- [INSTRUCTIONS_GUIDE.md](../INSTRUCTIONS_GUIDE.md) - How to create effective instructions
- [CHANGELOG.md](../CHANGELOG.md) - Detailed version history
- [IMPLEMENTATION_SUMMARY.md](../IMPLEMENTATION_SUMMARY.md) - Technical implementation details

## Contributing Guidelines

When extending or modifying this tool:

1. **Maintain single-file architecture**: Keep all code in `AssessRepo.cs`
2. **Follow existing patterns**: Use static methods, consistent naming
3. **Test thoroughly**: Run on multiple repositories
4. **Update documentation**: Modify this file and README.md
5. **Update version**: Increment version in report generation
6. **Maintain scoring balance**: Keep categories proportional (25/20/20/15/10 pattern)
7. **Consider Copilot CLI optional**: Tool must work without it

## Tool Philosophy

This tool embodies the principle: **"Don't just check for files, validate they're useful."**

Traditional assessments check: "Does `.github/copilot-instructions.md` exist?"

This tool asks: "Does Copilot actually understand the instructions in that file?"

By using Copilot CLI to test comprehension, we ensure repositories don't just have the right files, but have **effective** files that make Copilot genuinely helpful.
