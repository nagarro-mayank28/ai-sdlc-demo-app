# Copilot instructions

Follow `AGENTS.md` at the repository root; it is the single source of project rules (approval limits, layering, test command, review expectations).

- Spec Kit prompts are in `.github/prompts/speckit.*.prompt.md`.
- Agent roles, skills and hooks are defined under `.claude/` (agents, skills, settings.json). When using Copilot, follow the same agent role descriptions and log each prompt/response with `node scripts/ai-log.mjs` (see `.claude/skills/ai-interaction-log/SKILL.md`).
- Commit message format is enforced for everyone by `scripts/git-hooks/commit-msg` (enable with `git config core.hooksPath scripts/git-hooks`).
