# Repository Readiness Assessment Agent

## Purpose
This agent evaluates a repository's readiness for GitHub Copilot usage by assessing key factors that enable Copilot to effectively assist developers.

## Assessment Criteria

### 1. Build Capability (25 points)
**Can Copilot build the repo?**
- [ ] Build configuration files present (package.json, pom.xml, build.gradle, Makefile, etc.)
- [ ] Build instructions in README or documentation
- [ ] Dependencies clearly defined
- [ ] Build scripts are executable
- [ ] No hardcoded paths or environment-specific configurations
- [ ] Clear error messages for missing prerequisites

**Scoring:**
- All criteria met: 25 points
- Most criteria met: 18 points
- Some criteria met: 10 points
- Few/none met: 0 points

### 2. Run Capability (20 points)
**Can Copilot run the application?**
- [ ] Clear entry points identified (main files, start scripts)
- [ ] Runtime configuration documented
- [ ] Environment variables documented with examples (.env.example)
- [ ] Port numbers and service dependencies specified
- [ ] Launch configurations present (launch.json, docker-compose.yml)
- [ ] Minimal setup steps required

**Scoring:**
- All criteria met: 20 points
- Most criteria met: 14 points
- Some criteria met: 8 points
- Few/none met: 0 points

### 3. Test Capability (20 points)
**Can Copilot run tests?**
- [ ] Test framework configured
- [ ] Test command documented
- [ ] Test files follow clear naming conventions
- [ ] Test data/fixtures properly organized
- [ ] Integration tests separated from unit tests
- [ ] CI/CD configuration present

**Scoring:**
- All criteria met: 20 points
- Most criteria met: 14 points
- Some criteria met: 8 points
- Few/none met: 0 points

### 4. Code Understanding (20 points)
**Can Copilot understand the codebase?**
- [ ] Clear project structure and organization
- [ ] Meaningful file and folder names
- [ ] Code follows consistent style/conventions
- [ ] Comments explain "why" not "what"
- [ ] Complex logic is documented
- [ ] API contracts/interfaces well-defined
- [ ] Type annotations present (for applicable languages)

**Scoring:**
- All criteria met: 20 points
- Most criteria met: 14 points
- Some criteria met: 8 points
- Few/none met: 0 points

### 5. Documentation Quality (15 points)
**Is there sufficient context for Copilot?**
- [ ] README exists with project overview
- [ ] Architecture documentation present
- [ ] API documentation available
- [ ] Contributing guidelines exist
- [ ] Code of conduct present
- [ ] Changelog maintained
- [ ] Inline documentation for public APIs

**Scoring:**
- Comprehensive: 15 points
- Good: 10 points
- Basic: 5 points
- Minimal/none: 0 points

## Total Score: 100 points

### Grade Scale
- **A (90-100)**: Excellent - Copilot will thrive
- **B (80-89)**: Good - Minor improvements recommended
- **C (70-79)**: Fair - Several enhancements needed
- **D (60-69)**: Poor - Significant work required
- **F (0-59)**: Failing - Major restructuring needed

## Assessment Output Template

```markdown
# Repository Readiness Report

**Repository:** [Name]
**Date:** [Date]
**Overall Grade:** [A/B/C/D/F] ([Score]/100)

## Detailed Scores

### Build Capability: [Score]/25
- Strengths: [List]
- Weaknesses: [List]
- Recommendations: [List]

### Run Capability: [Score]/20
- Strengths: [List]
- Weaknesses: [List]
- Recommendations: [List]

### Test Capability: [Score]/20
- Strengths: [List]
- Weaknesses: [List]
- Recommendations: [List]

### Code Understanding: [Score]/20
- Strengths: [List]
- Weaknesses: [List]
- Recommendations: [List]

### Documentation Quality: [Score]/15
- Strengths: [List]
- Weaknesses: [List]
- Recommendations: [List]

## Priority Actions
1. [Highest priority fix]
2. [Second priority fix]
3. [Third priority fix]

## Conclusion
[Summary of readiness and key recommendations]
```

## Usage Instructions

1. Clone or access the target repository
2. Review each assessment criterion systematically
3. Check off applicable items
4. Calculate scores for each category
5. Generate the assessment report
6. Provide actionable recommendations

## Additional Considerations

### Copilot-Specific Features
- Presence of `.github/copilot-instructions.md` for workspace-specific guidance
- Clear separation of concerns in code structure
- Consistent patterns that Copilot can learn from
- Good test coverage that demonstrates expected behavior

### Red Flags
- Monolithic files over 1000 lines
- Deeply nested directory structures (>5 levels)
- Inconsistent coding styles
- Missing or outdated dependencies
- Broken builds in main branch
- No version control best practices

### Best Practices Bonus Points
- GitHub Actions workflows present (+5)
- Dependabot configuration (+3)
- CodeQL or security scanning (+3)
- Pre-commit hooks (+2)
- EditorConfig file (+2)

Maximum total with bonuses: 115 points
