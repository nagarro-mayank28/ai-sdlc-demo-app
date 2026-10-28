# Contract: Approvals API

All endpoints need `Authorization: Bearer <token>`. The class requires role Manager, Director or Admin (others get 403).

## POST /api/approvals/{invoiceId}/approve

Body: none. The approver is the signed-in user.

| Result | Status | Body |
| --- | --- | --- |
| Final approval | 200 | Invoice with `status: "Approved"` |
| First of two approvals | 200 | Invoice with `status: "AwaitingSecondApproval"`, `firstApprovedBy` |
| Refused by a limit or rule | 422 | `{ "error": "<reason>" }` |
| Invoice not pending | 409 | `{ "error": "..." }` |
| Invoice not found | 404 | `{ "error": "..." }` |

Example 422 reasons:

- `Invoice INV-1006 is 75,000.00, which is over the Manager approval limit of 50,000.00. A Director must approve it.`
- `demo.director already gave the first approval on invoice INV-1007; the second approval must come from a different person.`
- `The second approval on invoice INV-1007 (over 500,000.00) must come from a Director; demo.manager is a Manager.`
- `Role Admin has no approval authority.`

## POST /api/approvals/{invoiceId}/reject

Unchanged, but also accepted while `AwaitingSecondApproval`.
