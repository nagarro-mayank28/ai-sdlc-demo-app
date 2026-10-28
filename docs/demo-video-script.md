# Demo video script (8 to 10 minutes)

Record with Teams (start a meeting with only yourself, press Record) or OBS/any recorder that produces MP4. Keep a terminal, VS Code and a browser open. Things in **bold** must run live.

| Time | Show | Say (short) |
| --- | --- | --- |
| 0:00–0:45 | `ASSIGNMENT.md`, the feature's five rules | What was built and that the evidence matters more than the code. |
| 0:45–2:00 | `AGENTS.md` approval-rules + traps section; `.claude/skills/approval-rules`, `.claude/agents/`, `.claude/settings.json` hooks | How an agent arriving cold learns the rules, layering, test command and review expectations. Six sub-agents and the orchestrator command `/ship-feature`. |
| 2:00–2:45 | `specs/001-approval-limits/spec.md` Clarifications, `tasks.md` phases | Spec Kit output; the clarified ambiguities (inclusive limits, who gives the first approval, same-user comparison). |
| 2:45–3:30 | `git log --oneline main..HEAD`, then `git show --stat` on the two feat commits | Two implementation steps, each its own commit with a Why line. **Run** `git commit -m "wip"` to show the hook/commit-msg check rejecting a bad message. |
| 3:30–5:00 | **Run** `dotnet test backend/InvoiceApp.sln` | Point at `SameDirectorTwice_IsRefused_AndInvoiceIsUnchanged` (4 spellings) and `ManagerAsSecondApprover_IsRefused_AndFirstApprovalIsKept`. Live: change `OrdinalIgnoreCase` to `Ordinal` in `ApprovalLimits.IsSamePerson`, re-run, show the red tests, revert. |
| 5:00–6:30 | **Run** `bash scripts/mutation-before-after.sh` (or show the last run in `docs/mutation/scores.md` and run `cd backend && dotnet stryker -f stryker-config.json` live if time is short) | Before vs after score; one survivor and its verdict. |
| 6:30–7:45 | **Run** `cd e2e && npx playwright test --headed` | The manager sees the 50,000.00 reason and the invoice stays Pending Approval. |
| 7:45–9:15 | The PR on GitHub: description with every finding and its verdict | Why each was fixed, dismissed or deferred. |
| 9:15–9:45 | `ai-logs/ai-interactions.jsonl`, **run** `node scripts/ai-log-summary.mjs` | Every AI interaction logged in one format. |
| 9:45–10:00 | Wrap up | What I would do next (deferred items). |
