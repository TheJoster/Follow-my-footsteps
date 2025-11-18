# Key Libraries - OnlyHuman Project

> **Purpose**: This file defines the critical libraries and frameworks for this project.
> AI models should reference this file along with `claude.md` to understand the technical
> foundation and generate code consistent with the project's architecture.

---

## Core Dependencies

**Status**: ✅ Active Development

### Primary Framework
- **Unity** - Version 6000.0.25f1 (Unity 6)
  - Why: Industry-standard game engine with excellent 2D support, mobile optimization, and C# scripting
  - Use for: 2D hex-based RPG development with Standard Template Project (STP)
  - Docs: https://docs.unity3d.com/6000.0/Documentation/Manual/
  - Template: 2D (STP) - Standard 2D template optimized for mobile games
  - Platform: Windows development, targeting mobile deployment

### Backend/API
- **[Library Name]** - Version X.X.X
  - Why: [Reason]
  - Use for: [Purpose]
  - Docs: `[/org/repo]`

### Database & Data Layer
- **[Library Name]** - Version X.X.X
  - Why: [Reason]
  - Use for: [Purpose]
  - Docs: `[/org/repo]`

### Authentication & Security
- **[Library Name]** - Version X.X.X
  - Why: [Reason]
  - Use for: [Purpose]
  - Docs: `[/org/repo]`

---

## Development Tools

### Testing
- **Unity Test Framework** - Package `com.unity.test-framework`
  - Built-in NUnit 3.5 framework for EditMode and PlayMode tests
  - Use for: Unit tests (EditMode), Integration tests (PlayMode)
  - Requires: Manual installation via Package Manager
  - Target: 80%+ code coverage for core systems

### Code Quality
- **[Linter]** - Code style enforcement
- **[Formatter]** - Code formatting
- **[Type Checker]** - Static type checking

### Build & Bundling
- **[Build Tool]** - For production builds
- **[Dev Server]** - For development

---

## Integration Libraries

_Add third-party integrations as needed_

- **[Service/API Name]** - Purpose and client library

---

## AI Model Instructions

When generating code for this project:

1. **Always use the libraries listed above** - Do not suggest alternatives unless explicitly asked
2. **Follow version constraints** - Use the specified versions to avoid compatibility issues
3. **Check documentation** - Reference the `/org/repo` paths for accurate API usage
4. **Maintain consistency** - Use patterns established by these libraries throughout the codebase
5. **Validate imports** - Ensure all imports match the libraries defined here

---

## Planning Checklist

Before starting development, populate this file with:

- [ ] Primary framework and version
- [ ] Backend/API framework
- [ ] Database and ORM/query builder
- [ ] Authentication solution
- [ ] Testing frameworks
- [ ] Code quality tools (linter, formatter)
- [ ] Build and development tools
- [ ] Any critical third-party integrations

---

*Last updated: November 16, 2025*
*This file should be updated during project planning and whenever major dependencies change*
