# Implement next slice (API-first, always green)

READ OpenCode.md

Input:
- Slice plan (from `project:feature:plan-slice`)

Rules:
- Implement exactly the slice; do not expand scope.
- Keep the working tree green: if tests break, stop and fix immediately.
- Respect boundaries (Domain/Application free of EF Core + ASP.NET).

Implement in this order:
1) Tests (unit) for Domain/Application behavior
2) Domain changes (entities/invariants)
3) Application changes (DTOs, services, interfaces)
4) Infrastructure changes (repositories/migrations) only if needed for the slice
5) API endpoint wiring + error mapping
6) Tests (integration) only if needed to lock the HTTP contract

Validation:
RUN dotnet test

If tests fail:
- Use the smallest possible fix that preserves boundaries.
- Re-run `dotnet test` until green.
