using static InvoiceApp.Domain.ApprovalLimits;

namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through its lifecycle: Draft -> PendingApproval -> Approved or Rejected.
/// Approvals are limited by the approver's role (see <see cref="ApprovalLimits"/>).
/// Every rule is checked before the invoice is touched, so a refused approval changes nothing.
/// </summary>
public class ApprovalService
{
    private readonly IInvoiceRepository _repository;

    public ApprovalService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public Invoice Submit(string invoiceId)
    {
        var invoice = Find(invoiceId);

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Invoice {invoiceId} is {invoice.Status}; only drafts can be submitted.");
        }

        invoice.Status = InvoiceStatus.PendingApproval;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Approve(string invoiceId, Approver approver)
    {
        ArgumentNullException.ThrowIfNull(approver);

        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        var limit = SingleApproverLimitFor(approver.Role)
            ?? throw new ApprovalDeniedException($"Role {approver.Role} has no approval authority.");

        if (invoice.Amount > limit)
        {
            throw new ApprovalDeniedException(OverLimitReason(invoice, approver, limit));
        }

        // All checks passed: only now change the invoice.
        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approver.Username;
        invoice.DecidedAt = DateTime.UtcNow;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Reject(string invoiceId, string approver, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required to reject an invoice.", nameof(reason));
        }

        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        invoice.Status = InvoiceStatus.Rejected;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = DateTime.UtcNow;
        invoice.DecisionNote = reason;
        _repository.Update(invoice);
        return invoice;
    }

    private static string OverLimitReason(Invoice invoice, Approver approver, decimal limit)
    {
        var nextStep = invoice.Amount > DirectorLimit
            ? $"Invoices over {FormatAmount(DirectorLimit)} need two approvers."
            : "A Director must approve it.";

        return $"Invoice {invoice.Id} is {FormatAmount(invoice.Amount)}, which is over the " +
               $"{approver.Role} approval limit of {FormatAmount(limit)}. {nextStep}";
    }

    private Invoice Find(string invoiceId) =>
        _repository.GetById(invoiceId)
        ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

    private static void EnsurePending(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided.");
        }
    }
}
