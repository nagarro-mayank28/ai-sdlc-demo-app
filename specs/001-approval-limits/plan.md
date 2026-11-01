# Implementation Plan: Approval limits by role

**Branch**: `001-approval-limits` | **Date**: 2026-10-28 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-approval-limits/spec.md`

## Summary

Add role-based approval limits to `ApprovalService`. A new `ApprovalLimits` class in Domain holds the two thresholds and answers "what may this role approve". `ApprovalService.Approve` takes an `Approver` (username + role from the session), validates everything first, and only then mutates the invoice. Refusals throw `ApprovalDeniedException`, mapped to HTTP 422. Above 500,000.00 a new status `AwaitingSecondApproval` holds the first approval until a different Director completes it. Delivered in two commits: single-approver limits, then the two-approver rule.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core 8 (API), xUnit 2.9 (unit), Playwright 1.49 (e2e), Stryker.NET 4.13.0 (mutation)

**Storage**: In-memory `InMemoryInvoiceRepository` (returns live references, so validation-before-mutation matters)

**Testing**: `dotnet test backend/InvoiceApp.sln`; `cd e2e && npm test`; `cd backend && dotnet stryker -f stryker-config.json`

**Target Platform**: Local web server, http://localhost:5000

**Project Type**: Web service with static web UI

**Performance Goals**: N/A (demo)

**Constraints**: No new packages in Domain or Api. Money as `decimal`.

**Scale/Scope**: One service method changed, one new class, one new exception, one new status, UI tweaks.

## Constitution Check

| Principle | How this plan meets it |
| --- | --- |
| I. Rules in Domain | Limits in `ApprovalLimits`, decisions in `ApprovalService`; controller only builds `Approver` from the session. |
| II. Failure changes nothing | All checks run before any assignment; tests snapshot the invoice and count `Update` calls. |
| III. Test-first | Boundary + over-by-a-cent + negative tests per rule; Stryker before/after. |
| IV. Identity from session | `Approver` built from `HttpContext.GetSession()`; request body for approve stays empty. |
| V. Small increments | Phase 3 and Phase 4 of tasks.md are separate commits. |

Result: PASS, no violations.

## Project Structure

### Documentation (this feature)

```text
specs/001-approval-limits/
├── spec.md
├── plan.md
├── data-model.md
├── contracts/approvals-api.md
├── quickstart.md
└── tasks.md
```

### Source Code (repository root)

```text
backend/src/InvoiceApp.Domain/
├── ApprovalLimits.cs            (new: thresholds, role parsing)
├── Approver.cs                  (new: record Username + Role)
├── ApprovalDeniedException.cs   (new)
├── ApprovalService.cs           (changed: Approve(invoiceId, Approver))
├── Invoice.cs                   (changed: FirstApprovedBy/At)
└── InvoiceStatus.cs             (changed: AwaitingSecondApproval)
backend/src/InvoiceApp.Api/
├── Controllers/ApiControllerBase.cs   (422 mapping)
├── Controllers/ApprovalsController.cs (roles, Approver from session)
├── appsettings.json                   (demo.manager, demo.director, demo.director2)
└── wwwroot/app.js                     (awaiting-second list, alert)
backend/tests/InvoiceApp.Tests/
├── ApprovalServiceTests.cs
├── ApprovalLimitsTests.cs
├── FakeInvoiceRepository.cs     (UpdateCount)
└── InvoiceSnapshot.cs
backend/stryker-config.json
e2e/pages/*.ts, e2e/tests/over-limit-approval.spec.ts
```

**Structure Decision**: Existing layout kept (Domain / Api / Tests / e2e). No new projects.

## Complexity Tracking

None.
