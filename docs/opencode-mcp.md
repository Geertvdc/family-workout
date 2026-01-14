# opencode MCP servers (optional)

This repo works well with opencode using built-in tools and the `gh` CLI.

## When MCP is worth it
Use MCP when it provides a richer API than shelling out to CLI tools, or when you want typed/structured results (less parsing, fewer brittle flags).

## Suggested MCP servers
- **GitHub MCP (optional)**: Only needed if you want structured issue/PR/check interactions beyond what `gh` already covers for you.
- **PostgreSQL MCP (optional)**: Useful for schema inspection and debugging data-related integration tests.
- **Docker MCP (optional)**: If you want safer, structured container lifecycle operations from the agent.

## Rule of thumb
Prefer `dotnet test` + `gh` + local scripts first. Add MCP servers when you have a recurring workflow where CLI output parsing becomes annoying or error-prone.
