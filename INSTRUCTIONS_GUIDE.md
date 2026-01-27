# Creating Effective Custom Instructions for Copilot

## Quick Start Guide

This guide helps you create custom instructions that maximize your Copilot Readiness score by ensuring Copilot can **actually understand and use** your guidance.

## File Locations

### Repository-Level Instructions
```
.github/copilot-instructions.md          # Global instructions for entire repository
```

### Targeted Instructions
```
.github/instructions/
  ├── frontend.instructions.md           # For frontend code
  ├── backend.instructions.md            # For backend code
  ├── testing.instructions.md            # For test files
  └── deployment.instructions.md         # For CI/CD and deployment
```

## Scoring Criteria (Total: 15 points)

| Criteria | Points | Description |
|----------|--------|-------------|
| File presence | 3 | Instructions file(s) exist |
| Content length | 0-2 | >200 characters of meaningful content |
| Key sections | 0-5 | Contains Project, Tech Stack, Standards, Security, Testing |
| Copilot understanding | 0-7 | Copilot can extract and apply guidance |

## Template: Comprehensive Instructions

```markdown
---
description: Project-wide coding standards and guidelines
applyTo: "**/*"
---

# [Your Project Name]

## Project Overview
[2-3 sentences describing what this project does and who it's for]

Example:
> This is a customer relationship management system built for small businesses.
> It handles contact management, sales pipeline tracking, and email campaigns.

## Tech Stack

### Languages & Frameworks
- Primary: Python 3.11
- Web Framework: FastAPI 0.104
- Frontend: React 18 with TypeScript
- Database: PostgreSQL 15

### Key Dependencies
- Authentication: Auth0
- Payments: Stripe
- Email: SendGrid
- Hosting: AWS (ECS + RDS)

## Coding Standards

### Naming Conventions
- **Python**: snake_case for functions and variables, PascalCase for classes
- **TypeScript**: camelCase for functions/variables, PascalCase for components
- **Files**: Lowercase with hyphens (e.g., `user-service.py`)

### Code Organization
- Place business logic in `src/services/`
- API routes go in `src/routes/`
- Database models in `src/models/`
- Tests mirror source structure in `tests/`

### Required Patterns
- Always use async/await for I/O operations
- All API endpoints must include error handling
- Log errors using `src/utils/logger.py` (never `print()`)
- Use type hints in all Python functions

### Forbidden Patterns
- ❌ Never use `eval()` or `exec()`
- ❌ Don't hardcode credentials or API keys
- ❌ Avoid using `any` type in TypeScript
- ❌ No console.log in production code

## Security Practices

### Authentication
- All API endpoints require JWT authentication (except `/health`)
- Use `@require_auth` decorator from `src/auth/decorators.py`
- Token expiration: 1 hour for access, 7 days for refresh

### Input Validation
- Validate all user input with Pydantic models
- Sanitize inputs using `src/utils/validators.py`
- Never trust client-side validation alone

### Data Protection
- Encrypt PII at rest using AES-256
- Use parameterized queries for all database access
- Hash passwords with bcrypt (min 12 rounds)

## Testing Requirements

### Unit Tests
- Place in `tests/unit/`
- Use pytest with pytest-asyncio
- Minimum 80% code coverage required
- Run with: `pytest tests/unit/ --cov=src`

### Integration Tests
- Place in `tests/integration/`
- Use Docker Compose for dependencies
- Clean up test data in teardown
- Run with: `docker-compose -f docker-compose.test.yml up --abort-on-container-exit`

### Test Naming
- Format: `test_<function_name>_<scenario>_<expected_result>`
- Example: `test_create_user_with_invalid_email_raises_validation_error`

## Environment Configuration

### Required Variables
```bash
DATABASE_URL=postgresql://...
REDIS_URL=redis://...
AUTH0_DOMAIN=your-tenant.auth0.com
STRIPE_SECRET_KEY=sk_test_...
SENDGRID_API_KEY=SG....
```

### Local Development
1. Copy `.env.example` to `.env`
2. Fill in required values (see 1Password vault "Dev Credentials")
3. Never commit `.env` file

## Common Tasks

### Adding a New API Endpoint
1. Create route in `src/routes/`
2. Add Pydantic model for request/response
3. Implement business logic in `src/services/`
4. Add unit tests
5. Add integration test
6. Update API documentation

### Database Migrations
```bash
# Create migration
alembic revision --autogenerate -m "description"

# Review generated migration in alembic/versions/

# Apply migration
alembic upgrade head
```

## Documentation Standards

### Code Comments
- Use docstrings for all public functions/classes
- Format: Google-style docstrings
- Include Args, Returns, Raises sections

Example:
```python
def create_user(email: str, name: str) -> User:
    """Creates a new user account.
    
    Args:
        email: User's email address (must be unique)
        name: User's full name
        
    Returns:
        Newly created User object
        
    Raises:
        ValidationError: If email format is invalid
        DuplicateUserError: If email already exists
    """
```

## Related Resources
- [Architecture Decision Records](docs/adr/)
- [API Documentation](docs/api/)
- [Deployment Guide](docs/deployment.md)
- [Contributing Guidelines](CONTRIBUTING.md)
```

## What Makes Instructions Effective?

### ✅ Good Instructions (Score: 12-15 points)

**Specific and Actionable:**
```markdown
## Naming Conventions
- Use snake_case for Python functions
- Prefix private methods with underscore: `_internal_method()`
- Constants in UPPER_SNAKE_CASE
```

**Technology-Specific:**
```markdown
## Tech Stack
- FastAPI 0.104 (not Flask)
- SQLAlchemy 2.0 (use async patterns)
- Pydantic v2 for validation
```

**Clear Examples:**
```markdown
## Error Handling
Always wrap async database calls:
```python
try:
    result = await db.execute(query)
except SQLAlchemyError as e:
    logger.error(f"Database error: {e}")
    raise DatabaseException("Query failed")
```
```

### ❌ Poor Instructions (Score: 0-5 points)

**Too Generic:**
```markdown
## Coding Standards
- Write clean code
- Follow best practices
- Use proper naming
```
*Problem: Copilot can't extract specific guidance*

**No Tech Stack:**
```markdown
## Overview
This is a web application.
```
*Problem: Copilot doesn't know Python vs. JavaScript vs. Java*

**Missing Security:**
```markdown
## Instructions
Build features according to requirements.
```
*Problem: No guidance on authentication, validation, or data protection*

## Testing Your Instructions

After creating instructions, run the assessment tool:

```bash
dotnet script AssessRepo.cs -- "C:\path\to\your\repo" --verbose
```

Look for this output:
```
[6/8] Assessing Custom Instructions Quality...
  Asking Copilot to interpret custom instructions...
  ✓ Copilot demonstrates excellent understanding of instructions
  
Custom Instructions: 15/15
```

If score is low, check:
1. **Are instructions specific?** Replace generic terms with exact commands/patterns
2. **Is tech stack clear?** List versions and primary frameworks
3. **Are standards actionable?** Provide code examples
4. **Is content substantial?** Aim for 1000+ characters

## Targeted Instructions with YAML Frontmatter

For path-specific guidance:

```markdown
---
description: Frontend component standards
applyTo: "src/components/**/*.tsx"
---

# Frontend Component Guidelines

## Component Structure
- Use functional components with hooks
- Props: Define with TypeScript interface
- Styling: Use Tailwind CSS classes only

## Naming
- PascalCase: `UserProfileCard.tsx`
- Props interface: `UserProfileCardProps`
```

## Custom Agents

Create specialized agents in `.github/agents/`:

```markdown
---
name: APIReviewer
description: Reviews API endpoints for security and standards compliance
target: github-copilot
tools: ["read", "edit"]
infer: true
---

# API Reviewer Agent

When reviewing API endpoints:

1. Check authentication decorator is present
2. Verify input validation with Pydantic
3. Ensure error handling wraps all async calls
4. Confirm logging for errors
5. Check rate limiting is configured
```

## Agent Skills

Create reusable skills in `.github/skills/`:

```
.github/skills/
  └── api-endpoint/
      └── SKILL.md
```

```markdown
---
name: CreateAPIEndpoint
description: Scaffold a new FastAPI endpoint with all required patterns
---

# Create API Endpoint Skill

## Usage
"Create a new API endpoint for [resource] with [methods]"

## Pattern
1. Create route file in src/routes/
2. Add Pydantic models for request/response
3. Implement service in src/services/
4. Add authentication decorator
5. Include error handling
6. Add unit tests
```

## Continuous Improvement

Run assessments regularly and iterate:

1. **Baseline**: Initial assessment
2. **Add Instructions**: Create `.github/copilot-instructions.md`
3. **Re-assess**: Check score improvement
4. **Refine**: Add missing sections or clarify vague guidance
5. **Validate**: Confirm Copilot understands (score 12+/15)

---

## Quick Checklist

Before committing your instructions:

- [ ] File is at `.github/copilot-instructions.md`
- [ ] Includes project overview (purpose + audience)
- [ ] Lists tech stack with versions
- [ ] Defines coding standards (naming, organization, patterns)
- [ ] Documents security practices
- [ ] Explains testing requirements
- [ ] Content is >500 characters
- [ ] Uses specific examples, not generic advice
- [ ] Tested with assessment tool (score 12+/15)

---

## Need Help?

Run the assessment tool to see exactly what Copilot understands:

```bash
dotnet script AssessRepo.cs -- "." --verbose
```

Check the report section:
- **Copilot Understanding Test**: Shows what Copilot extracted
- **Recommendations**: Specific improvements to make
