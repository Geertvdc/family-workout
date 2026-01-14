# Iterate feature spec (API-first)

READ OpenCode.md

Goal: Maintain a short living spec that evolves iteratively.

Input you should assume (ask if missing):
- Feature name
- Current progress (what exists today)
- New learnings since last iteration
- The next user-facing outcome we want

Produce a spec in this exact template (keep it <= 1 page):

## Spec vN: <Feature Name>

### Goal
- <one sentence>

### User flows
- <bullets>

### Acceptance criteria
- [EXISTING] ...
- [NEW] ...

Rules:
- Prefer 6–10 criteria total.
- Add at most 1–2 [NEW] criteria per iteration.
- Each criterion must be testable (unit/integration) or explicitly “manual”.

### Non-goals (for now)
- <bullets>

### Open questions
- <bullets, max 5>

### Risks / assumptions
- <bullets>

Then, select exactly 1–2 acceptance criteria to target next and output:

## Next focus
- <criterion id/name> — why now
