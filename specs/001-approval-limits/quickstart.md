# Quickstart: try approval limits

1. `dotnet run --project backend/src/InvoiceApp.Api`, open http://localhost:5000
2. Sign in as `demo.manager` / `ChangeMe123!`. Create an invoice for 75,000, submit it, open Approvals, click Approve → the red alert explains the 50,000.00 limit; the invoice stays pending.
3. Sign out, sign in as `demo.director`. Approve it → Approved.
4. Create and submit a 750,000 invoice. Approve as `demo.director` → "Awaiting second approval (first: demo.director)". Approve again → refused (same person).
5. Sign in as `demo.director2`, approve → Approved.
6. `dotnet test backend/InvoiceApp.sln` and `cd e2e && npm test`.
