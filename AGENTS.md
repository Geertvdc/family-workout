# Agent entrypoint

This repo is optimized for opencode-style coding agents.

## Start here
- Project memory: `OpenCode.md`
- Guidelines: `.dev/coding-agents/coding-agent-instructions.md`
- Architecture overview: `.dev/coding-agents/architecture-overview.md`

## Default workflow
- Prefer `dotnet test` over running the full app.
- Keep changes minimal and focused.
- Avoid architectural drift: keep Domain/Application free of EF Core + ASP.NET.

## Useful commands
- Run tests: `dotnet test`
- Build: `dotnet build`

## Project commands (opencode)
Project commands live in `.opencode/commands/` and show up as `project:*` in opencode.
