# OnlyHuman - AI Development Guide

## Project Overview
OnlyHuman is framework to guide AI models in code generations following human requirements on various do's and don'ts.

### Tech Stack
- **Language**: Python, C#, HTML, CSS, Flutter, Dart
- **Framework**: Flutter, .NET, Unity, WinUI
- **Database**: Azure SQL, Azure mySQL
- **Key Libraries**: This will differ per project, so look for a file called KeyLibraries.md

## Code Standards

### General Principles
1. **Clarity over cleverness** - Write code that is easy to understand and maintain
2. **DRY (Don't Repeat Yourself)** - Extract common patterns into reusable functions/components
3. **Single Responsibility** - Each function/class should have one clear purpose
4. **Fail fast** - Validate inputs early and provide clear error messages
5. **Document intent** - Use comments to explain *why*, not *what*
6. **Do not reinvent the wheel** - Leverage MCP servers like Context7 to retrieve documentation related to the application intent and enhance the code base accordantly.

### Naming Conventions
- **Variables**: Use descriptive names that reveal intent (e.g., `userCount` not `uc`)
- **Functions**: Use verbs that describe what they do (e.g., `calculateTotal`, `fetchUserData`)
- **Classes**: Use nouns in PascalCase (e.g., `UserRepository`, `PaymentProcessor`)
- **Constants**: Use UPPER_SNAKE_CASE (e.g., `MAX_RETRY_ATTEMPTS`)
- **Files**: Use kebab-case for files (e.g., `user-service.ts`, `payment-utils.py`)

### Code Organization
```
/src
  /components     # Reusable UI components
  /services       # Business logic and API calls
  /utils          # Helper functions and utilities
  /types          # Type definitions and interfaces
  /config         # Configuration files
  /tests          # Test files (mirror src structure)
```

### Function Guidelines
- Keep functions small (< 50 lines ideally)
- Maximum 3-4 parameters (use objects for more)
- Return early to reduce nesting
- Use guard clauses for validation
- Avoid side effects where possible

### Error Handling
- Always validate user inputs
- Use try-catch blocks for operations that can fail
- Provide meaningful error messages
- Log errors with sufficient context
- Never swallow errors silently

## Documentation Requirements

### Code Comments
- Add docstrings/JSDoc to all public functions
- Include:
  - Brief description of purpose
  - Parameter types and descriptions
  - Return type and description
  - Example usage (for complex functions)
  - Any side effects or important notes

Example:
```typescript
/**
 * Calculates the total price including tax and discounts
 * @param basePrice - The original price before tax/discounts
 * @param taxRate - Tax rate as decimal (e.g., 0.21 for 21%)
 * @param discountCode - Optional discount code to apply
 * @returns The final price after all calculations
 * @throws {ValidationError} If basePrice is negative
 */
function calculateFinalPrice(
  basePrice: number,
  taxRate: number,
  discountCode?: string
): number {
  // Implementation
}
```

### README Updates
When adding new features, update:
- Installation instructions (if dependencies change)
- Usage examples
- API documentation
- Configuration options

## Testing Standards

### Test Coverage
- Aim for 80%+ code coverage
- Every public function should have tests
- Test edge cases and error conditions
- Use descriptive test names: `test_calculateTotal_withNegativeValue_throwsError`

### Test Structure
```
// Arrange - Set up test data
const user = createTestUser();

// Act - Execute the function
const result = processUser(user);

// Assert - Verify the outcome
expect(result.status).toBe('active');
```

## Git Workflow

### Commit Messages
Follow conventional commits format:
```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

Examples:
```
feat(auth): add password reset functionality
fix(api): resolve timeout issue in user endpoint
docs(readme): update installation instructions
```

### Branch Strategy
- `main` - Production-ready code
- `develop` - Integration branch
- `feature/[name]` - New features
- `fix/[name]` - Bug fixes
- `refactor/[name]` - Code improvements

## AI Code Generation Guidelines

### When Requesting Code
1. **Be specific about requirements**
   - Input/output expectations
   - Edge cases to handle
   - Performance requirements
   - Error scenarios

2. **Provide context**
   - Related code files
   - Existing patterns in the codebase
   - Integration points

3. **Request explanation**
   - Ask for code walkthrough
   - Request design decision rationale
   - Get suggestions for improvements

### Code Review Checklist
When AI generates code, verify:
- [ ] Follows naming conventions
- [ ] Includes proper error handling
- [ ] Has appropriate documentation
- [ ] Includes relevant tests
- [ ] No hardcoded values (use config/env)
- [ ] Handles edge cases
- [ ] Performance is acceptable
- [ ] Security considerations addressed
- [ ] Accessibility standards met (if UI)

## Security Considerations

### Always Validate
- User inputs
- API responses
- File uploads
- Query parameters

### Never Commit
- API keys or secrets
- Passwords or tokens
- Personal data
- Database credentials

Use environment variables and `.env` files (add `.env` to `.gitignore`)

### Common Vulnerabilities to Avoid
- SQL injection (use parameterized queries)
- XSS attacks (sanitize user input)
- CSRF (use tokens)
- Insecure dependencies (keep updated)

## Performance Guidelines

### Optimization Principles
- Measure before optimizing
- Focus on algorithmic improvements first
- Cache expensive operations
- Use pagination for large datasets
- Lazy load when appropriate
- Minimize network requests

### Database Queries
- Use indexes on frequently queried columns
- Avoid N+1 queries
- Use connection pooling
- Implement query timeouts

## Accessibility (A11y)

For UI components:
- Use semantic HTML
- Include ARIA labels where needed
- Ensure keyboard navigation works
- Maintain color contrast ratios (WCAG AA)
- Provide text alternatives for images
- Test with screen readers

## Progressive Enhancement

### Build for All Users
1. Start with working HTML/basic functionality
2. Add CSS for enhanced presentation
3. Layer JavaScript for interactivity
4. Ensure graceful degradation

## Code Review Process

### Before Submitting
1. Self-review your changes
2. Run all tests, use independent subagents to run the critical tests
3. Check for console errors/warnings
4. Verify documentation is updated
5. Ensure commits are clean and descriptive

### Seeking Feedback
- Ask specific questions
- Provide context for changes
- Link to related issues/tickets
- Highlight areas of uncertainty

## Resources and References

### Style Guides
- [Link to language-specific style guide]
- [Link to framework best practices]

### Learning Materials
- [Link to relevant documentation]
- [Link to tutorials or courses]

### Tools
- Linter: [e.g., ESLint, Pylint]
- Formatter: [e.g., Prettier, Black]
- Type Checker: [e.g., TypeScript, mypy]

---

## Quick Reference Card

### Before Writing Code
1. Understand the requirement fully
2. Check existing patterns in codebase
3. Plan the approach
4. Consider edge cases

### While Writing Code
1. Follow naming conventions
2. Keep functions small and focused
3. Add comments for complex logic
4. Handle errors appropriately

### After Writing Code
1. Write/update tests
2. Update documentation
3. Self-review changes
4. Run linter and tests (for tests verify with independent subagents)
5. Commit with clear message

---

*Last updated: November 16, 2025*
*This document should evolve with the project*
