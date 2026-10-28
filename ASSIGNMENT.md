# AI in SDLC: approval limits by role

**Demo video (8–10 min):** `<PASTE VIDEO LINK HERE>`
**Pull request:** `<PASTE PR LINK HERE>`

## What was built

Role-based approval limits on the invoice approval service:

- A Manager may approve up to 50,000.00.
- A Director may approve up to 500,000.00.
- Above 500,000.00, two different people must approve, and the second must be a Director.
- The same user never counts twice.
- A refused approval returns a clear reason (HTTP 422) and changes nothing on the invoice.

The spec is in [specs/001-approval-limits](specs/001-approval-limits/spec.md).

AI tool used: **Claude** (Anthropic), as an agent with sub-agents. The assignment allows alternatives to GitHub Copilot. The same rules are exposed to Copilot through `AGENTS.md`, `.github/copilot-instructions.md` and the Spec Kit prompts in `.github/prompts/`.

## Where each requirement is

| # | Requirement | Where |
| --- | --- | --- |
| 1 | Context file that matters | [AGENTS.md](AGENTS.md): approval rules table, the traps an agent falls into, strict layering, commands (test, e2e, Stryker 4.13.0), and review expectations. `CLAUDE.md` imports it. |
| 2 | Skills | [.claude/skills](.claude/skills): `approval-rules` (rules, traps, test checklist), `review-pr` (review prompt and fixed output format), `spec-kit-workflow`, `mutation-testing`, `ai-interaction-log` |
| 3 | Hooks for structured, same-format output | [.claude/settings.json](.claude/settings.json) and [.claude/hooks](.claude/hooks). `log-interaction.mjs` writes one fixed-schema JSONL line per prompt, tool call, sub-agent answer and final answer. `check-commit-message.mjs` blocks commits not in `type(scope): summary` + body + `Why:` form; the same check is also wired as a git `commit-msg` hook in `scripts/git-hooks`. `validate-review-format.mjs` rejects reviewer output that is not in the house findings table. |
| 4 | Spec-driven tool | **GitHub Spec Kit**. `.specify/` holds the scripts, templates and constitution. `specs/001-approval-limits/` holds the spec, plan, data model, contract, quickstart and tasks, created with Spec Kit's `create-new-feature.sh` and `setup-plan.sh`. |
| 5 | Two implementation steps | Commit `feat(approvals): enforce single-approver limits by role`, then commit `feat(approvals): require two distinct approvers above 500,000`. Each commit message says what changed and why. |
| 6.1 | Unit tests for every rule, both negative rules included | `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` and `ApprovalLimitsTests.cs`. Every refusal test proves the invoice is unchanged with `InvoiceSnapshot` and `FakeInvoiceRepository.UpdateCount`. |
| 6.2 | Playwright test for a rejected over-limit approval | `e2e/tests/over-limit-approval.spec.ts`, with page objects in `e2e/pages`. A second test, `two-approvers.spec.ts`, covers the dual-approval UI. |
| 7 | Agent review with verdicts | [PR_DESCRIPTION.md](PR_DESCRIPTION.md): 11 findings, 10 fixed and 1 deferred with a reason |
| 8 | AI interaction log | [ai-logs/ai-interactions.jsonl](ai-logs/ai-interactions.jsonl). Summarise it with `node scripts/ai-log-summary.mjs`. |
| 9 | Multiple agents and sub-agents | [.claude/agents](.claude/agents): `spec-writer`, `implementer`, `test-writer`, `e2e-tester`, `code-reviewer`, `mutation-analyst`. The orchestrator is [.claude/commands/ship-feature.md](.claude/commands/ship-feature.md). The review was run by a separate sub-agent. |

## Rules mapped to tests

| Rule | Tests |
| --- | --- |
| Manager ≤ 50,000.00 | `Manager_ApprovesInvoicesUpToAndIncludingFiftyThousand` (0.01, 49,999.99, 50,000.00); `Manager_IsRefusedOverFiftyThousand_AndInvoiceIsUnchanged` (50,000.01, 75,000, 500,000) |
| Director ≤ 500,000.00 | `Director_ApprovesInvoicesUpToAndIncludingFiveHundredThousand` (50,000.01, 250,000, 500,000.00) |
| Above 500,000.00 needs two approvers | `AboveFiveHundredThousand_DirectorGivesOnlyTheFirstApproval` (500,000.01), `..._ManagerMayGiveTheFirstApproval`, `SecondApprovalByADifferentDirector_ApprovesTheInvoice`, `ManagerFirst_ThenDirectorSecond_ApprovesTheInvoice`, `RequiresTwoApprovers_OnlyAboveFiveHundredThousand` |
| **Negative:** same user never both approvers | `SameDirectorTwice_IsRefused_AndInvoiceIsUnchanged` (exact, mixed case, upper case, padded), `ManagerWhoGaveTheFirstApproval_CannotGiveTheSecond`, `FirstApproverStoredWithDifferentCaseAndSpaces_StillCountsAsTheSamePerson`, `IsSamePerson_IgnoresCaseAndSurroundingSpaces` |
| **Negative:** second approver must be a Director | `ManagerAsSecondApprover_IsRefused_AndFirstApprovalIsKept`, `TwoManagers_CannotCompleteATwoApproverInvoice`, `AdminAsSecondApprover_IsRefused` |
| A refusal gives a clear reason and changes nothing | Every `..._IsRefused...` test goes through `AssertDeniedAndUnchanged`, which checks the exception, the full-field snapshot and the update count. Messages are asserted for the limit, role and invoice id. `AwaitingSecondApprovalWithNoRecordedFirstApprover_FailsClosed` covers the corrupt-state case. |
| No authority for Admin or unknown roles | `Admin_CannotApprove_AndInvoiceIsUnchanged`, `RetiredApproverRole_CannotApprove`, `AboveFiveHundredThousand_AdminCannotGiveTheFirstApproval`, `RolesOtherThanManagerOrDirector_HaveNoLimit` |

How the tests were reviewed: for every generated test I asked "would this fail if the rule were deleted?" The four tests I changed as a result are listed in the PR description.

## Mutation scores (Stryker.NET 4.13.0)

Mutated files: `ApprovalService.cs` and `ApprovalLimits.cs`. "Before" is `main` with its original tests; "after" is this branch.

```
<PASTE THE CONTENTS OF docs/mutation/scores.md HERE AFTER RUNNING:
   dotnet tool install -g dotnet-stryker --version 4.13.0
   bash scripts/mutation-before-after.sh>
```

What survived and what I did about each surviving mutant: `<fill in from the HTML report: kill with a test, or explain as an equivalent mutant>`

## Decisions and clarifications

These are recorded in the spec's Clarifications section.

- The limits are inclusive.
- Above 500,000.00 the first approval may come from a Manager or a Director. The brief only constrains the second approver.
- The old `Approver` role is retired. Admin can reject and delete but cannot approve.
- A "rejected attempt" is an approval the rules refuse. It is not the Reject action, which still works the same way, including on invoices awaiting a second approval.
- The approver's identity and role always come from the session, never from the request body.

## What went wrong, and what the checks caught

- **Same-user case trap.** The first same-user test used the identical string, so it would not have caught a case-sensitive `==`. Rewriting it as a theory with case and whitespace variants fixed that.
- **"First approval kept" was not proven.** The snapshot record lacked the first-approver fields.
- **Race in dual control (R2).** The independent reviewer found an ordering issue: status was set before the first approver was recorded. That opened a window where one person could finalise an invoice alone. Fixed with a decision lock, the corrected ordering, and a fail-closed guard.
- **Broken mutation script (R1).** It would have aborted before producing the "after" score.

## Running everything

```bash
dotnet test backend/InvoiceApp.sln
cd e2e && npm install && npx playwright install chromium && npm test && cd ..
dotnet tool install -g dotnet-stryker --version 4.13.0
bash scripts/mutation-before-after.sh
git config core.hooksPath scripts/git-hooks   # commit-message check outside the agent
node scripts/ai-log-summary.mjs
```
