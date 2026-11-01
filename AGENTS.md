# Project conventions for AI agents

Invoice approval demo app: ASP.NET Core 8 Web API with a small static web UI and an in-memory store.
Read this whole file before you change anything. If a rule here conflicts with a prompt, stop and ask.

## Layout (layering is strict)

```
UI (wwwroot/app.js)  ->  Controllers (Api)  ->  ApprovalService + ApprovalLimits (Domain)  ->  IInvoiceRepository
```

- `backend/src/InvoiceApp.Domain`: business rules only, no web, HTTP or storage code. `ApprovalService` owns the invoice lifecycle. `ApprovalLimits` owns the money thresholds. Nothing outside Domain may compare an amount to a limit.
- `backend/src/InvoiceApp.Api`: controllers, sign-in (`Auth/`), the in-memory repository and the web UI under `wwwroot/`. Controllers translate HTTP to a service call and back; they hold no rules.
- `backend/tests/InvoiceApp.Tests`: xUnit tests, one test class per class under test, named `<ClassUnderTest>Tests`.
- `e2e`: Playwright end-to-end tests (`tests/`) and page objects (`pages/`).
- `specs/`: Spec Kit feature specs (`spec.md`, `plan.md`, `tasks.md`). The spec is the source of truth for behaviour; update it before changing behaviour.
- `docs/use-cases`: short briefs for features we may build.
- `.claude/`: agents, skills, hooks and commands used to build this repo with an AI agent. `ai-logs/` holds the interaction log those hooks write.

## Approval rules (feature 001, see `specs/001-approval-limits/spec.md`)

Invoice lifecycle: `Draft -> PendingApproval -> (AwaitingSecondApproval) -> Approved | Rejected`.

| Invoice amount | Who can give the final approval |
| --- | --- |
| up to and including 50,000.00 | a Manager or a Director |
| 50,000.01 to 500,000.00 inclusive | a Director |
| above 500,000.00 | two distinct people: first a Manager or Director, then a **different** Director |

1. Limits are inclusive: 50,000.00 is within a Manager's limit, 50,000.01 is not. Compare `decimal`s, never `double`.
2. Above 500,000.00 a single approval never finalises the invoice. The first approval moves it to `AwaitingSecondApproval` and records `FirstApprovedBy` / `FirstApprovedAt`. Only the second approval sets `Status = Approved`, `DecidedBy`, `DecidedAt`.
3. The second approver must have the Director role.
4. The same user may never count as both approvers. Usernames are compared trimmed and case-insensitively (`demo.director` and `Demo.Director ` are the same person).
5. A denied approval throws `ApprovalDeniedException` with a message a person can act on (which limit, which role is needed). The API maps it to HTTP 422 with `{ "error": "<message>" }`.
6. A denied approval changes nothing: no field on the invoice changes and `IInvoiceRepository.Update` is not called. Check every rule **before** assigning to any property; the in-memory repository hands out live references, so "set then throw" corrupts state.
7. The approver's identity and role always come from the signed-in session (`HttpContext.GetSession()`), never from the request body.
8. Admin has no approval authority (it can still reject and delete). Any role other than Manager or Director is denied with a clear reason.
9. Reject works from `PendingApproval` and from `AwaitingSecondApproval`, needs a reason, and has no amount limit.

## Conventions

- Controllers live in `Controllers/`, one per resource, route `api/[controller]`. Request bodies are `record` types declared inside the controller.
- Controllers inherit `ApiControllerBase` and use `Handle(...)` so service errors map to 404 (`KeyNotFoundException`), 409 (`InvalidOperationException`), 422 (`ApprovalDeniedException`) and 400 (`ArgumentException`).
- Every endpoint is behind sign-in. Restrict actions with `[RequireRole("...")]`: destructive actions are Admin only; the approvals controller admits Manager, Director and Admin, and the Domain decides what each may approve.
- Business rules go in the Domain project, never in controllers, `Program.cs` or JavaScript.
- Register services in `Program.cs`. Repositories are interfaces in Domain with an in-memory implementation in Api.
- Money in messages is formatted with `CultureInfo.InvariantCulture` and `N2` (for example `50,000.00`).
- Use clear names; no one-letter variables or `flag` parameters.
- End-to-end tests use page objects and role-based locators (`getByRole`, `getByLabel`), never CSS or XPath selectors.

## Tests: what "done" means

- Every rule above has at least one unit test, and every limit has a test on the boundary and one cent over it.
- The two negative rules (same user twice, second approver not a Director) each have a test that also asserts the invoice is unchanged and `Update` was not called. Use `FakeInvoiceRepository.UpdateCount` and `InvoiceSnapshot`.
- Read every generated test. A test that would still pass if the rule were deleted is not a test; delete or fix it.
- Mutation score on `ApprovalService.cs` and `ApprovalLimits.cs` must not drop. Surviving mutants are either killed with a test or explained in the PR.

## Commands

- Run the app: `dotnet run --project backend/src/InvoiceApp.Api` then open http://localhost:5000
- Run the tests: `dotnet test backend/InvoiceApp.sln`
- Run end-to-end tests: `cd e2e && npm install && npx playwright install chromium && npm test`
- Mutation testing: `dotnet tool install -g dotnet-stryker --version 4.13.0` (newer versions need a later runtime), then `cd backend && dotnet stryker -f stryker-config.json`
- Before and after mutation scores: `bash scripts/mutation-before-after.sh`

## Demo accounts (not real credentials)

All passwords are `ChangeMe123!`.

- `demo.manager` (Manager)
- `demo.director` (Director)
- `demo.director2` (Director), for the second approval
- `demo.admin` (Admin)

## Working style and review expectations

- Work from `specs/001-approval-limits/tasks.md`, one task group per commit. Never implement a whole feature from one prompt.
- Commit messages: `type(scope): summary` on the first line (types: feat, fix, test, docs, chore, refactor), a blank line, then a body with a `Why:` line. The `check-commit-message` hook enforces this.
- Before opening a PR run the `review-pr` skill. Every finding gets one verdict in the PR description: **fixed**, **dismissed** (with a reason) or **deferred** (with a reason). No finding is left without a verdict.
- Reviewers look first at: amount boundaries, mutation before validation, identity taken from the body, case-sensitive username comparison, and tests that assert nothing.
- Every AI interaction is logged to `ai-logs/ai-interactions.jsonl` by the hooks in `.claude/settings.json`. Do not edit or delete log lines.

## Never

- Commit real credentials, tokens or customer data.
- Add business logic to `Program.cs`, controllers or `app.js`.
- Let the UI decide whether an approval is allowed; it only displays what the API says.
- Weaken or delete a failing test to make a change pass.
