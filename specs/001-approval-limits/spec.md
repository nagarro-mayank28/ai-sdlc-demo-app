# Feature Specification: Approval limits by role

**Feature Branch**: `001-approval-limits` (git branch `feature/approval-limits`)

**Created**: 2026-10-28

**Status**: Implemented

**Input**: User description: "Add approval limits by role to the invoice approval service. A Manager may approve invoices up to 50,000. A Director may approve invoices up to 500,000. Anything above 500,000 needs two distinct approvers, and the second approver must be a director. The same user may never count as both approvers on one invoice. A rejected attempt returns a clear reason, and nothing about the invoice changes."

## Clarifications

### Session 2026-10-28

- Q: Are the limits inclusive (can a Manager approve exactly 50,000)? → A: Yes. "Up to 50,000" means `amount <= 50,000.00`; 50,000.01 is over. Same for 500,000.00.
- Q: For invoices above 500,000, who may give the *first* approval? → A: A Manager or a Director. The brief only constrains the second approver; a Manager first approval models the budget owner signing off before director sign-off. Admin and unknown roles may not give either approval.
- Q: Can a single Director approve an invoice above 500,000? → A: No. They can give the first approval only; the invoice waits for a second, different Director.
- Q: What counts as "the same user"? → A: Same username after trimming whitespace, compared case-insensitively.
- Q: What does "rejected attempt" mean, since the app also has a Reject action? → A: An *approval attempt the rules refuse* (over limit, wrong role, same person twice). It is reported as an error with a reason; the invoice's status and every other field stay as they were. It is not the same as the Reject decision, which still exists and is unchanged.
- Q: What happens to the existing `Approver` role? → A: It is replaced by `Manager` and `Director`. Admin keeps its rights to reject and delete but has no approval authority.
- Q: Can an invoice awaiting its second approval still be rejected? → A: Yes, by any Manager, Director or Admin, with a reason, as today.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Single approver within their limit (Priority: P1)

A Manager approves invoices up to 50,000 and a Director up to 500,000. An approval over the approver's limit is refused with a reason and the invoice stays pending.

**Why this priority**: It is the core control; without it anyone can approve any amount.

**Independent Test**: Sign in as a Manager, try to approve a 75,000 invoice: it is refused with a message naming the 50,000.00 limit, and the invoice is still Pending Approval.

**Acceptance Scenarios**:

1. **Given** a pending invoice of 50,000.00, **When** a Manager approves it, **Then** it is Approved and the Manager is recorded as the decider.
2. **Given** a pending invoice of 50,000.01, **When** a Manager approves it, **Then** the approval is refused with a reason naming the Manager limit and that a Director must approve, and the invoice is unchanged.
3. **Given** a pending invoice of 500,000.00, **When** a Director approves it, **Then** it is Approved.
4. **Given** a pending invoice of any amount, **When** an Admin approves it, **Then** the approval is refused with a reason saying the role has no approval authority, and the invoice is unchanged.

---

### User Story 2 - Two approvers above 500,000 (Priority: P2)

Invoices above 500,000 need two distinct people; the second must be a Director.

**Why this priority**: Large payments need dual control; depends on Story 1's roles and limits.

**Independent Test**: Approve a 750,000 invoice as `demo.director` (it becomes Awaiting Second Approval), try again as `demo.director` (refused), then approve as `demo.director2` (Approved).

**Acceptance Scenarios**:

1. **Given** a pending invoice of 500,000.01, **When** a Director approves it, **Then** it moves to Awaiting Second Approval with that Director recorded as first approver, and it is not Approved.
2. **Given** a pending invoice of 750,000, **When** a Manager approves it, **Then** it moves to Awaiting Second Approval with the Manager as first approver.
3. **Given** an invoice awaiting second approval from Director A, **When** a different Director B approves it, **Then** it is Approved, B is the decider, and A stays recorded as first approver.
4. **Given** an invoice awaiting second approval from Director A, **When** Director A approves it again (with any letter case or surrounding spaces in the username), **Then** it is refused with a reason saying the same person cannot give both approvals, and the invoice is unchanged.
5. **Given** an invoice awaiting second approval from Director A, **When** a Manager approves it, **Then** it is refused with a reason saying the second approval must come from a Director, and the invoice is unchanged (first approval kept).
6. **Given** an invoice awaiting second approval, **When** it is rejected with a reason, **Then** it is Rejected.

---

### User Story 3 - See why an approval was refused in the UI (Priority: P3)

An approver who clicks Approve on an invoice above their limit sees the reason on screen, and the invoice stays in the pending list.

**Independent Test**: Playwright: Manager creates and submits a 75,000 invoice, clicks Approve, sees the alert naming the 50,000.00 limit, and the invoice still shows as Pending Approval.

**Acceptance Scenarios**:

1. **Given** a Manager on the Approvals tab with a 75,000 invoice pending, **When** they click Approve, **Then** an alert shows the reason and the invoice remains listed as pending.
2. **Given** an invoice awaiting second approval, **When** anyone views the Approvals tab, **Then** it shows who gave the first approval and that a Director is needed.

### Edge Cases

- Amount exactly at a limit, and one cent over it.
- Approving an invoice that is already Approved or Rejected → existing 409 conflict, unchanged.
- Approving an invoice that does not exist → existing 404.
- Usernames differing only by case or whitespace count as the same person.
- A refused second approval must not wipe the recorded first approval.
- Role names from configuration that are neither Manager nor Director (for example the retired `Approver`) have no authority.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow a Manager to give a final approval only when the invoice amount is at most 50,000.00.
- **FR-002**: System MUST allow a Director to give a final approval alone only when the amount is at most 500,000.00.
- **FR-003**: System MUST require two approvals for amounts above 500,000.00: the first from a Manager or Director, the second from a Director.
- **FR-004**: System MUST refuse a second approval from the same user who gave the first (trimmed, case-insensitive username match).
- **FR-005**: System MUST refuse approvals from any role other than Manager or Director.
- **FR-006**: A refused approval MUST return a human-readable reason naming the rule (limit and role needed) and MUST leave every field of the invoice unchanged and not write to storage.
- **FR-007**: System MUST record the first approver and time on two-approver invoices and keep them after final approval.
- **FR-008**: The API MUST return HTTP 422 with `{ "error": "<reason>" }` for a refused approval.
- **FR-009**: The approver identity and role MUST come from the signed-in session.
- **FR-010**: Reject MUST remain available on pending and awaiting-second-approval invoices with no amount limit.
- **FR-011**: The web UI MUST show the refusal reason and list invoices awaiting a second approval with their first approver.

### Key Entities

- **Invoice**: amount, status (Draft, PendingApproval, AwaitingSecondApproval, Approved, Rejected), first approver and time, final decider and time, decision note.
- **Approver**: username and role (Manager, Director, Admin) from the session.
- **Approval limits**: Manager 50,000.00; Director 500,000.00; above that, two approvers.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of acceptance scenarios above are covered by automated tests (unit, plus one end-to-end for Story 3).
- **SC-002**: Mutation score on the approval rule code is reported before and after and does not drop.
- **SC-003**: No refused approval in any test changes any invoice field.

## Assumptions

- Amounts are in a single currency; no currency conversion.
- Limits are fixed in code for this release (not configurable per tenant).
- Bulk approval (UC-1) and partial approval (UC-2) are out of scope.
- Demo users are configured in `appsettings.json`; roles there are the roles used.
