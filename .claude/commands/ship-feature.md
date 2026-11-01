---
description: Orchestrate a feature through spec, two-step implementation, tests, e2e, mutation and review using the project sub-agents.
argument-hint: <feature description>
---

You are the orchestrator. Do not write the code yourself; delegate to the sub-agents in `.claude/agents/` and check their work.

Feature: $ARGUMENTS

1. **spec-writer**: create/update `specs/` with spec, plan and tasks. Read the Clarifications and confirm them with the user.
2. **implementer** with phase "single-approver limits", then **test-writer** for that phase. Review every test it wrote against the `approval-rules` checklist. Commit.
3. **implementer** with phase "two-approver rule", then **test-writer**. Review the two negative-rule tests line by line. Commit.
4. **e2e-tester**: one Playwright test for a rejected over-limit approval. Commit.
5. **mutation-analyst**: before/after scores; send survivors to **test-writer**.
6. **code-reviewer**: review `git diff main...HEAD`. For every finding decide fixed / dismissed / deferred and write the verdict table into the PR description.

Every step is logged automatically by the hooks to `ai-logs/ai-interactions.jsonl`.
