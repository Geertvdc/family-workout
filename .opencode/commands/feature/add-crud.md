# Add CRUD feature (safe default)

READ OpenCode.md
READ docs/datamodel.md
READ docs/api-endpoints.md

Implement in this order (stop and ask if scope is unclear):
1) Domain: add/adjust entity invariants
2) Application: DTOs + service method(s) + repository interface additions
3) Infrastructure: EF Core mapping/repository implementation + migrations if needed
4) API: minimal endpoints + validation + error mapping
5) Blazor: page/forms using API over HTTP
6) Tests: unit tests first; add integration tests only if needed

Rules:
- Keep changes minimal.
- Do not introduce new dependencies without asking.
- Prefer `dotnet test` for validation.
