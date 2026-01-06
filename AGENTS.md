# Instructions for AI Coding Agents

Welcome! Before you start working on this project, please follow these important guidelines:

## 📚 Required Reading

**STOP!** Before writing any code, you MUST read the documentation in the `.dev/coding-agents/` directory:

1. **[Architecture Overview](.dev/coding-agents/architecture-overview.md)** - Understand the Clean Architecture pattern, technology stack (PostgreSQL, EF Core, .NET 10, Blazor), and data flow.

2. **[Coding Agent Instructions](.dev/coding-agents/coding-agent-instructions.md)** - Follow coding standards, error handling patterns, testing guidelines, and EF Core best practices.

3. **[Task Definitions](.dev/coding-agents/tasks/)** - Review completed and upcoming tasks to understand the project context.

## 🎯 Core Principles

- **Keep It Simple (KISS)**: Don't add features or complexity that aren't explicitly needed
- **Clean Architecture**: Respect layer boundaries (Domain → Application → Infrastructure → API/UI)
- **Test-Driven Development (TDD)**: Write tests before implementation
- **Immutable Domain Entities**: Use constructors for validation and `With*` methods for updates
- **PostgreSQL + EF Core**: Use Entity Framework Core with Fluent API for database access

## 🏗️ Technology Stack

- **Backend**: .NET 10 with Minimal APIs
- **Frontend**: Blazor (Interactive Server)
- **Database**: PostgreSQL 17 with Entity Framework Core
- **Local Dev**: .NET Aspire for orchestration
- **Testing**: xUnit with in-memory repositories

## 🚫 Common Pitfalls to Avoid

1. Don't add dependencies from Domain to Application/Infrastructure layers
2. Don't put business logic in API endpoints
3. Don't skip validation in Domain entity constructors
4. Don't use property setters in Domain entities
5. Don't add features "just in case" - wait for actual requirements
6. Don't forget to configure entities using Fluent API in DbContext
7. Don't skip writing tests before implementing features

## 📖 Additional Resources

- **[README.md](README.md)** - Project overview, setup instructions, running locally
- **[QUICKSTART.md](QUICKSTART.md)** - Quick start guide
- **[docs/](docs/)** - API endpoints, data model, CI/CD documentation

## ✅ Before You Start Coding

- [ ] I have read the architecture overview
- [ ] I have read the coding agent instructions
- [ ] I understand the Clean Architecture pattern used in this project
- [ ] I understand the TDD approach required
- [ ] I know which layer my changes belong to
- [ ] I will write tests before implementation

---

**Remember**: Quality over speed. Take time to understand the architecture and follow the established patterns. This ensures maintainability and consistency across the codebase.
