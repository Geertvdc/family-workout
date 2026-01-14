# OpenCode Project Memory

## What this repo is
FamilyFitness is a .NET 10 solution with Clean Architecture-ish layering:
- Domain: `src/FamilyFitness.Domain`
- Application: `src/FamilyFitness.Application`
- Infrastructure (EF Core + PostgreSQL): `src/FamilyFitness.Infrastructure`
- API (Minimal APIs): `src/FamilyFitness.Api`
- Blazor UI: `src/FamilyFitness.Blazor`
- Tests: `tests/*`

## Golden commands (default)
- Run tests: `dotnet test`
- Build: `dotnet build`

Prefer these over running the full app stack (Aspire) unless asked.

## Architecture invariants (enforced)
We prefer preventing architectural drift over fast feature delivery.

### Option A boundaries (current repo reality)
- Domain must not depend on Application/Infrastructure/API/Blazor.
- Application must not depend on Infrastructure/API/Blazor.
- Infrastructure must not depend on API/Blazor.
- API must not depend on Blazor.
- Blazor must not depend on Infrastructure.

### Framework drift prevention
- Domain must not depend on EF Core or ASP.NET.
- Application must not depend on EF Core or ASP.NET.

## Testing policy
- Prefer unit tests for business logic (Application services + Domain).
- Add integration tests only when the behavior cannot be validated in unit tests.
- Keep tests deterministic (no reliance on time/network unless isolated).

## Existing docs (source of truth)
- Quickstart: `QUICKSTART.md`
- Repo overview: `README.md`
- Data model: `docs/datamodel.md`
- CI behavior: `docs/CI.md`
- Agent-focused guidelines: `.dev/coding-agents/coding-agent-instructions.md`
- Architecture overview (agent-focused): `.dev/coding-agents/architecture-overview.md`

## Common pitfalls
- Don’t introduce Domain → EF Core / ASP.NET dependencies.
- Don’t put business rules in `src/FamilyFitness.Api`.
- Don’t bypass Application services by calling Infrastructure directly from UI.
