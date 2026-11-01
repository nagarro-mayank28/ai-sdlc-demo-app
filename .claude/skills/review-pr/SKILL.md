---
name: review-pr
description: Use when asked to review a pull request, a branch or a diff in this repo. Runs the review prompt against the diff and returns findings in the one fixed table format the validate-review-format hook checks.
---

# Review a pull request

## Steps

1. Get the diff: `git diff main...HEAD` (or the range you were given). Read the spec at `specs/001-approval-limits/spec.md` and the rules in `AGENTS.md`.
2. Run the review prompt below against the diff. Read surrounding code where the diff alone is not enough.
3. Output **only** the format below. The `validate-review-format` hook rejects anything else.
4. The author then gives every finding a verdict in the PR description: **fixed** (commit), **dismissed** (reason) or **deferred** (reason and where it is tracked).

## Review prompt

> You are reviewing a pull request on an invoice approval service. Review the diff for:
> 1. **Correctness against the spec**: every acceptance scenario in the spec, especially boundaries (exactly at a limit, one cent over), the two-approver rule, and the rule that the same user never counts twice.
> 2. **Failure behaviour**: a denied approval must return a clear reason and leave the invoice completely unchanged. Look for state assigned before a check, and for exceptions that leave partial state.
> 3. **Security**: identity and role taken from the request instead of the session; role checks that can be bypassed; missing `[RequireRole]`.
> 4. **Layering**: business rules in controllers, `Program.cs` or JavaScript; thresholds duplicated outside `ApprovalLimits`.
> 5. **Tests**: rules with no test; tests that would pass if the rule were removed; missing negative tests; e2e tests using CSS/XPath selectors.
> 6. **Maintainability**: naming, dead code, misleading comments, inconsistent error messages.
>
> Do not praise. Do not restate the diff. Each finding must name a file and line, say what is wrong and why it matters, and suggest a concrete fix. If you are not sure, say so in the finding and mark it `low`.

## Output format (exactly this)

```
## Review findings

| ID | Severity | File:line | Finding | Suggested fix |
| --- | --- | --- | --- | --- |
| R1 | high | backend/src/InvoiceApp.Domain/ApprovalService.cs:42 | ... | ... |

Summary: <one sentence>
```

- Severity is one of `blocker`, `high`, `medium`, `low`, `nit`.
- IDs run R1, R2, ... in order of severity.
- No pipes `|` inside a cell; use commas.
- If there is nothing to report write `No findings.` under the table header, then the Summary line.

## Verdict table for the PR description

```
| ID | Finding (short) | Verdict | Reason / commit |
| --- | --- | --- | --- |
| R1 | ... | fixed | abc1234 |
| R2 | ... | dismissed | <why it is not a problem> |
| R3 | ... | deferred | <why not now, and the follow-up> |
```
