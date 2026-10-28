# Approval limits by role (spec 001)

Implements [specs/001-approval-limits/spec.md](specs/001-approval-limits/spec.md):

- A Manager may approve invoices up to and including 50,000.00.
- A Director may approve invoices up to and including 500,000.00.
- Above 500,000.00 two different people must approve. The first can be a Manager or a Director; the second must be a Director.
- The same user never counts as both approvers. Usernames are compared trimmed and case-insensitively.
- A refused approval returns HTTP 422 with a reason and changes nothing on the invoice.

## Commits

| Commit | What |
| --- | --- |
| `docs(agents)` | AGENTS.md rules, layering and review expectations; `.claude` skills, hooks and sub-agents; AI interaction log |
| `docs(spec)` | Spec Kit scaffold, plus spec, plan, data model, contract and tasks |
| `feat(approvals): enforce single-approver limits by role` | **Step 1**: limits, `ApprovalDeniedException` → 422, approver taken from the session, new demo users, unit tests |
| `feat(approvals): require two distinct approvers above 500,000` | **Step 2**: `AwaitingSecondApproval`, first approver, same-person rule, Director-second rule, UI, unit tests |
| `test(e2e)` | Playwright test: a refused over-limit approval, checked through the UI |
| `docs(demo)` | Video script |
| `fix(approvals)` | Fixes for the review findings below |

## How to verify

```bash
dotnet test backend/InvoiceApp.sln
cd e2e && npm install && npx playwright install chromium && npm test
bash scripts/mutation-before-after.sh
```

## Agent review: findings and verdicts

The `code-reviewer` sub-agent ran the `review-pr` prompt (`.claude/skills/review-pr/SKILL.md`) against `git diff main...HEAD`. It is independent: it did not write this code. The `validate-review-format` hook checked the shape of its output. Every finding below has a verdict.

| ID | Sev | Finding (short) | Verdict | Reason / commit |
| --- | --- | --- | --- | --- |
| R1 | high | `mutation-before-after.sh` copies `stryker-config.json` onto itself on the "after" run. Under `set -e` the script aborts before writing any scores. | **fixed** | The copy now runs only for the baseline worktree. `fix(approvals)` |
| R2 | medium | `GiveFirstApproval` sets `Status` before `FirstApprovedBy`. With live in-memory references, a concurrent second request from the same Director could read `FirstApprovedBy == null`, pass the same-person check and finalise alone. | **fixed** | Three changes: a static decision lock around approve and reject; the first approver is recorded before the status changes; the service refuses when an awaiting invoice has no first approver. New test: `AwaitingSecondApprovalWithNoRecordedFirstApprover_FailsClosed`. With a real database this becomes an optimistic-concurrency check, out of scope here. `fix(approvals)` |
| R3 | medium | US3 scenario 2 (the awaiting-second-approval display) has no test; the UI note could be deleted and every test would still pass. | **fixed** | Added `e2e/tests/two-approvers.spec.ts`. It covers the first approval, the display, the refused same-Director retry and the second Director approving. `fix(approvals)` |
| R4 | low | The no-authority message names neither the invoice nor the roles that can approve (FR-006). | **fixed** | The message now names the invoice, the user, the role, and says that only a Manager or Director can approve. A test asserts each part. `fix(approvals)` |
| R5 | low | Trimming is tested only on the second username. Removing `firstUsername.Trim()` would not fail any test. | **fixed** | Added `IsSamePerson` cases with a padded or uppercased first argument, and a service test whose first approver is `" Demo.Director "`. `fix(approvals)` |
| R6 | low | Some message fragments are never asserted, so string mutants would survive. | **fixed** | Tests now assert the invoice id, `over 500,000.00` and the username in the denial messages. `fix(approvals)` |
| R7 | low | `app.js` hard-codes the rule text "A different Director must approve", which duplicates a Domain rule in the UI. | **fixed** | The UI now shows only data from the API: `Awaiting second approval (first: X)`. The rule text comes from the API's 422 reason. `fix(approvals)` |
| R8 | low | The README lifecycle and test commands are out of date. | **fixed** | Added the new status and the e2e and mutation commands. `fix(approvals)` |
| R9 | low | UC-1 still says "over $1,000 needs a second approver", which conflicts with this spec. | **fixed** | Added a note in UC-1 that dual approval now follows spec 001. The UC-1 brief itself stays as written because it is a separate, unbuilt feature. `fix(approvals)` |
| R10 | nit | A contract example used INV-1006, which is now a seeded 750,000 invoice. | **fixed** | The examples now use ids that match the seed. `fix(approvals)` |
| R11 | nit | `stryker-config.json` has `"break": 0`, so a falling score never fails a run. | **deferred** | The baseline score has not been measured on a real run yet, and setting a threshold blind would either never fire or always fail. Follow-up: once `docs/mutation/scores.md` exists, set `break` just below the "after" score and add it to CI. |

## Tests reviewed by hand

These are the generated tests I changed after reading them. The interaction log has the full notes.

- The same-user test first used only the identical string `demo.director`, so a naive `==` would have passed. It is now a theory with case and whitespace variants.
- `InvoiceSnapshot` first omitted `FirstApprovedBy` and `FirstApprovedAt`, so the tests did not really prove the first approval was kept. Both fields are now in the snapshot.
- One generated test asserted the full message string, which is brittle. It now uses `Contains` on the parts a user relies on: the limit, the role and the rule.
- The first e2e draft used a CSS selector `#decision-error`. It now uses `getByRole('alert')` scoped to the Approvals tab panel.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_01KLx2udwXtSvWNyDGmBDmfG
