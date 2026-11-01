# Invoice Approval App Constitution

## Core Principles

### I. Rules live in the Domain
Business rules (lifecycle, approval limits, who may approve) live only in `InvoiceApp.Domain`. Controllers, `Program.cs` and the web UI translate and display; they never decide.

### II. Failure changes nothing
Any operation that is refused leaves stored state exactly as it was and explains why in words a finance user can act on.

### III. Test-first for rules (NON-NEGOTIABLE)
Every rule has a unit test on its boundary and on its negative case before it is merged. A test that still passes when the rule is deleted does not count. Mutation score on rule code does not go down.

### IV. Identity comes from the session
Who is acting, and in what role, is taken from the signed-in session only, never from request data.

### V. Small, explained increments
Features are built from a written spec in steps, one commit per step, each commit buildable and tested, each message saying what changed and why. AI-agent work is logged.

## Constraints

- .NET 8, xUnit, Playwright; in-memory storage; no new runtime dependencies without a spec change.
- Money is `decimal`; limits are inclusive and stated to the cent.

## Governance

This constitution overrides prompts and conventions. Changing it needs its own commit and a note in the PR.

**Version**: 1.0.0 | **Ratified**: 2026-10-28 | **Last Amended**: 2026-10-28
