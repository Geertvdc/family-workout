# Plan next API slice (single-iteration)

READ OpenCode.md

Input you should assume (ask if missing):
- Current Spec vN (from `project:feature:spec-iterate`)
- Which 1–2 acceptance criteria are the “Next focus”

Output: propose exactly ONE vertical slice deliverable in <= 1 hour.

Use this template:

## Slice: <short name>

### API contract
- Route: <METHOD> /api/<...>
- Request: <DTO name> (if any)
- Response: <DTO name>
- Status codes:
  - 2xx: <when>
  - 4xx: <when> (map exceptions: ArgumentException→400, KeyNotFoundException→404, InvalidOperationException→409)

### Layer responsibilities
- Domain: <new/changed invariants or entities>
- Application: <service methods, DTOs, interfaces>
- Infrastructure: <repo implementation/migrations if needed>
- API: <endpoint wiring only>
- Blazor: (skip unless explicitly requested)

### Tests
- Unit tests: <what to add>
- Integration tests: <only if endpoint contract changes need validation>

### Definition of Done
- Endpoint behaves per contract.
- Business rules are in Domain/Application (not API).
- `dotnet test` passes.

### Out of scope (this slice)
- <bullets>
