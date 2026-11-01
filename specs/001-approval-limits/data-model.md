# Data model: Approval limits by role

## Invoice (changed)

| Field | Type | Notes |
| --- | --- | --- |
| Status | InvoiceStatus | adds `AwaitingSecondApproval` between `PendingApproval` and `Approved` |
| FirstApprovedBy | string? | new; set by the first approval on invoices > 500,000.00 |
| FirstApprovedAt | DateTime? | new; UTC |
| DecidedBy / DecidedAt | string? / DateTime? | unchanged meaning: who made the final decision |

## State transitions

```
Draft --submit--> PendingApproval
PendingApproval --approve (amount <= 500,000.00, within role limit)--> Approved
PendingApproval --approve (amount > 500,000.00, Manager or Director)--> AwaitingSecondApproval
AwaitingSecondApproval --approve (different user, Director)--> Approved
PendingApproval | AwaitingSecondApproval --reject (reason)--> Rejected
any refused approval --> (no change)
```

## Approver (new, value)

`Approver(Username, Role)`, built from the session. Roles: `Manager`, `Director`, `Admin` (no approval authority).

## ApprovalLimits (new)

| Role | Single-approver limit |
| --- | --- |
| Manager | 50,000.00 |
| Director | 500,000.00 |
| other | none |

`RequiresTwoApprovers(amount) = amount > 500,000.00`
