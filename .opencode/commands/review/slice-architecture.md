# Review slice architecture (boundaries + placement)

READ OpenCode.md

Input:
- The proposed slice plan (from `project:feature:plan-slice`)

Output:

## Boundary check
- Domain: allowed changes / forbidden changes
- Application: allowed changes / forbidden changes
- Infrastructure: allowed changes / forbidden changes
- API: allowed changes / forbidden changes
- Blazor: allowed changes / forbidden changes

## Placement decisions
- Business rules live in: Domain/Application
- Persistence mapping lives in: Infrastructure
- HTTP concerns live in: API

## Drift risks (top 3)
- <bullets>

## Green-light checklist
- [ ] No EF Core / ASP.NET dependencies in Domain/Application
- [ ] API only wires HTTP → Application
- [ ] DTOs consistent across endpoints
- [ ] Exceptions map to HTTP status codes
