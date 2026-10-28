---

description: "Task list for approval limits by role"
---

# Tasks: Approval limits by role

**Input**: Design documents from `/specs/001-approval-limits/`

**Prerequisites**: plan.md, spec.md, data-model.md, contracts/approvals-api.md

**Tests**: Required by the constitution (principle III) and the assignment: unit tests for every rule including both negative rules, one Playwright test, mutation before/after.

**Organization**: One phase per commit. Phase 3 and Phase 4 are the two implementation steps and must be separate commits.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1, US2, US3 from spec.md

---

## Phase 1: Setup

- [x] T001 Extend `AGENTS.md` with approval rules, layering, commands and review expectations
- [x] T002 [P] Add `.claude/` skills, hooks (`settings.json`), sub-agents and the AI interaction log
- [x] T003 [P] Add Spec Kit scaffold (`.specify/`) and this spec, plan and task list

---

## Phase 2: Foundational

- [ ] T004 [P] Add `backend/stryker-config.json` and `scripts/mutation-before-after.sh`; record the baseline mutation score on `main`
- [ ] T005 [P] Add `UpdateCount` to `backend/tests/InvoiceApp.Tests/FakeInvoiceRepository.cs` and `InvoiceSnapshot.cs` so tests can prove "nothing changed"

**Checkpoint**: tests can prove a refusal changed nothing.

---

## Phase 3: User Story 1 - Single approver within their limit (P1) 🎯 MVP  — commit 1

**Goal**: Manager ≤ 50,000.00, Director ≤ 500,000.00, everything else refused with a reason and no change.

### Tests for User Story 1

- [ ] T006 [P] [US1] `ApprovalLimitsTests.cs`: limit per role, unknown role has none, boundaries
- [ ] T007 [P] [US1] `ApprovalServiceTests.cs`: Manager at 50,000.00 approves; at 50,000.01 refused, unchanged, `UpdateCount == 0`; Director at 50,000.01 and 500,000.00 approves; Admin and retired `Approver` refused; existing tests moved to `Approver`

### Implementation for User Story 1

- [ ] T008 [P] [US1] `Approver.cs`, `ApprovalDeniedException.cs`, `ApprovalLimits.cs` in Domain
- [ ] T009 [US1] `ApprovalService.Approve(string invoiceId, Approver approver)`: all checks before any assignment
- [ ] T010 [US1] `ApiControllerBase.Handle`: map `ApprovalDeniedException` to 422
- [ ] T011 [US1] `ApprovalsController`: roles Manager, Director, Admin; build `Approver` from the session
- [ ] T012 [US1] `appsettings.json`, `README.md`: `demo.manager`, `demo.director`, `demo.director2`, `demo.admin`

**Checkpoint**: single-approver limits work end to end; `dotnet test` green.

---

## Phase 4: User Story 2 - Two approvers above 500,000 (P2)  — commit 2

**Goal**: Above 500,000.00 two distinct approvers, second a Director; same user never twice.

### Tests for User Story 2

- [ ] T013 [US2] First approval (Director or Manager) moves to `AwaitingSecondApproval`, not Approved, records first approver
- [ ] T014 [US2] Different Director completes it; first approver kept
- [ ] T015 [US2] **Negative**: same Director twice refused (exact, different case, padded) — reason, unchanged, `UpdateCount == 0`
- [ ] T016 [US2] **Negative**: Manager as second approver refused — reason, unchanged, first approval kept
- [ ] T017 [US2] Admin as first approver refused; reject while awaiting second approval works; approve after Approved is 409

### Implementation for User Story 2

- [ ] T018 [US2] `InvoiceStatus.AwaitingSecondApproval`; `Invoice.FirstApprovedBy`, `FirstApprovedAt`
- [ ] T019 [US2] `ApprovalLimits.RequiresTwoApprovers`, `ApprovalLimits.IsSamePerson`
- [ ] T020 [US2] `ApprovalService`: first/second approval paths; `EnsureDecidable` accepts both pending states
- [ ] T021 [US2] `wwwroot/app.js` + `styles.css`: list awaiting-second invoices with first approver

**Checkpoint**: all unit tests green; manual quickstart steps 4–5 pass.

---

## Phase 5: User Story 3 - Refusal visible in the UI (P3)  — commit 3

- [ ] T022 [P] [US3] `e2e/pages/LoginPage.ts`, `InvoicesPage.ts`, `ApprovalsPage.ts` with role-based locators
- [ ] T023 [US3] `e2e/tests/over-limit-approval.spec.ts`: Manager approves a 75,000 invoice → alert with reason, invoice still pending

---

## Phase 6: Polish

- [ ] T024 Run the `review-pr` skill via the `code-reviewer` sub-agent; give every finding a verdict in `PR_DESCRIPTION.md`
- [ ] T025 Fix accepted findings in their own commit
- [ ] T026 Mutation score after; write `ASSIGNMENT.md`

## Dependencies

Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6. Within a phase, `[P]` tasks touch different files.
