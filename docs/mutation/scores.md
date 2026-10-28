# Mutation scores (Stryker.NET 4.13.0)

Mutated files: ApprovalService.cs, ApprovalLimits.cs. Score = (killed + timeout) / (killed + timeout + survived + no coverage).

| Run | Commit | Score | Mutant statuses | Files with tested mutants |
| --- | --- | --- | --- | --- |
| before | 66cb3f9 (main) | 60.00% | Killed 12, Ignored 31, Survived 8 | ApprovalService.cs |
| after | 137a4c4 (HEAD) | 86.67% | Killed 52, Ignored 41, Survived 8 | ApprovalLimits.cs, ApprovalService.cs |

Surviving mutants after the change:

```
ApprovalService.cs:33  Survived  String mutation  $"Invoice {invoiceId} is {invoice.Status}; only drafts can be submitted."  ->  $""
ApprovalService.cs:37  Survived  Statement mutation  _repository.Update(invoice);  ->  ;
ApprovalService.cs:93  Survived  String mutation  "A reason is required to reject an invoice."  ->  ""
ApprovalService.cs:99  Survived  Statement mutation  EnsureDecidable(invoice);  ->  ;
ApprovalService.cs:105  Survived  Statement mutation  _repository.Update(invoice);  ->  ;
ApprovalService.cs:129  Survived  String mutation  "it cannot be approved until this is corrected."  ->  ""
ApprovalService.cs:160  Survived  String mutation  $"Invoice {invoiceId} was not found."  ->  $""
ApprovalService.cs:167  Survived  String mutation  $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided."  ->  $""
8 surviving mutant(s).
```
