---
name: approval-rules
description: Use when changing, testing or reviewing anything about invoice approval limits, roles, the two-approver rule or ApprovalService. Gives the rules, the traps an agent tends to fall into, and the test checklist.
---

# Invoice approval rules

The source of truth is `specs/001-approval-limits/spec.md`. This skill is the working summary.

## Rules

| Amount (decimal, inclusive bounds) | Final approval needs |
| --- | --- |
| `<= 50,000.00` | one Manager or Director |
| `50,000.01 .. 500,000.00` | one Director |
| `> 500,000.00` | first: Manager or Director; second: a **different** Director |

- Thresholds live only in `ApprovalLimits` (`ManagerLimit`, `DirectorLimit`). Do not repeat the numbers anywhere else in code.
- Above the director limit the first approval sets `Status = AwaitingSecondApproval`, `FirstApprovedBy`, `FirstApprovedAt`. `DecidedBy`/`DecidedAt` stay null until the second approval.
- Same person check: `string.Equals(first.Trim(), second.Trim(), StringComparison.OrdinalIgnoreCase)`.
- Denials throw `ApprovalDeniedException` (HTTP 422). Its message names the invoice, the amount, the limit and what is needed next.
- Admin, or any unknown role, has no approval authority.

## Traps (check each one before you say "done")

1. **Set-then-throw.** Assigning `Status` (or anything) before the last check means a denied approval still changes the live invoice in the in-memory repository. All checks first, then all assignments, then `Update`.
2. **`<` vs `<=`.** 50,000.00 is allowed for a Manager. 500,000.00 is a single Director approval. Test both edges and one cent over.
3. **Director finalises alone above 500k.** "Director can approve up to 500k" is easy to read as "Director can approve anything". Above 500k one Director only gives the first approval.
4. **Same user, different case or spaces.** `demo.director` vs `Demo.Director` must be caught.
5. **Second approver role.** A Manager must not complete a two-approver invoice, even after a Director gave the first approval.
6. **First approval lost on denial.** A denied second attempt must keep `FirstApprovedBy` intact.
7. **Identity from the body.** The approver always comes from the session.

## Test checklist

For each rule write the test, then ask: "if I deleted the line that enforces this, would the test fail?" If not, fix the test.

- [ ] Manager at 50,000.00 approves; at 50,000.01 is denied
- [ ] Director at 500,000.00 approves alone; at 500,000.01 only gives a first approval
- [ ] Two distinct directors complete a > 500k invoice
- [ ] Manager first + Director second completes it
- [ ] Same user twice is denied (exact and different case/whitespace)
- [ ] Manager as second approver is denied
- [ ] Admin / unknown role is denied
- [ ] Every denial test asserts `InvoiceSnapshot.Of(invoice)` is unchanged and `repository.UpdateCount == 0`
- [ ] Every denial test asserts the message says why (contains the limit or the rule)
