---
name: spec-kit-workflow
description: Use when starting or changing a feature: write or update the Spec Kit spec, plan and tasks under specs/ before writing code, and implement tasks one group per commit.
---

# Spec Kit workflow

This repo uses GitHub Spec Kit (`.specify/`). Commands are in `.github/prompts/speckit.*.prompt.md` for Copilot and `.claude/commands/speckit.*.md` for Claude Code.

1. `/speckit.constitution`: principles live in `.specify/memory/constitution.md`. Read them; do not rewrite them per feature.
2. `/speckit.specify <feature description>`: runs `.specify/scripts/bash/create-new-feature.sh --json --short-name <name> "<description>"`, which creates `specs/NNN-name/spec.md` from `.specify/templates/spec-template.md`. Fill user stories, acceptance scenarios (Given/When/Then), functional requirements (FR-xxx) and edge cases. No implementation detail.
3. `/speckit.clarify`: list open questions; record each answer in the spec's `## Clarifications` section.
4. `/speckit.plan`: `plan.md` from the plan template: technical context, constitution check, files touched, data model, API contract.
5. `/speckit.tasks`: `tasks.md`, grouped by phase/user story, each task with an ID (T001), `[P]` if parallel, and a file path.
6. `/speckit.implement`: one phase per prompt and per commit. Tick tasks (`[x]`) in the same commit as the code.

Commit the generated files. A behaviour change without a spec change is a review finding.
