# Architecture review checklist

READ OpenCode.md

Check these before finalizing:
- Domain does not depend on Application/Infrastructure/API/Blazor.
- Application does not depend on Infrastructure/API/Blazor.
- Infrastructure does not depend on API/Blazor.
- API does not depend on Blazor.
- Blazor does not depend on Infrastructure.
- No EF Core / ASP.NET dependencies in Domain or Application.

Also check:
- Public APIs return consistent DTOs.
- Exceptions map to HTTP status codes in API.
- Tests cover the new behavior.

RUN dotnet test
