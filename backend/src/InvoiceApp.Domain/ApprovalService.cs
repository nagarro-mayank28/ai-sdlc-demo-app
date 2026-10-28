using static InvoiceApp.Domain.ApprovalLimits;

namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through its lifecycle:
/// Draft -> PendingApproval -> (AwaitingSecondApproval) -> Approved or Rejected.
/// Approvals are limited by the approver's role (see <see cref="ApprovalLimits"/>); above the director
/// limit two different people must approve and the second must be a Director.
/// Every rule is checked before the invoice is touched, so a refused approval changes nothing.
/// </summary>
public class ApprovalService
{
    // The in-memory repository hands out live references and the service is created per request,
    // so decisions are serialised here: two concurrent approvals of one invoice cannot interleave
    // between a check and the change it guards.
    private static readonly object DecisionLock = new();

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

        lock (DecisionLock)
        {
            return ApproveUnderLock(invoiceId, approver);
        }
    }

    private Invoice ApproveUnderLock(string invoiceId, Approver approver)
    {
        var invoice = Find(invoiceId);
        EnsureDecidable(invoice);

        // Written as an explicit null check rather than "?? throw" so mutation testing can mutate this
        // method: Stryker's mutant of "?? throw" leaves a decimal? that does not compile, and Stryker then
        // drops every mutant in the method.
        var roleLimit = SingleApproverLimitFor(approver.Role);
        if (roleLimit is null)
        {
            throw new ApprovalDeniedException(
                $"Invoice {invoice.Id} cannot be approved by {approver.Username}: role {approver.Role} " +
                "has no approval authority. Only a Manager or a Director can approve invoices.");
        }

        var limit = roleLimit.Value;

        if (invoice.Status == InvoiceStatus.AwaitingSecondApproval)
        {
            return GiveSecondApproval(invoice, approver);
        }

        if (RequiresTwoApprovers(invoice.Amount))
        {
            return GiveFirstApproval(invoice, approver);
        }

        if (invoice.Amount > limit)
        {
            throw new ApprovalDeniedException(
                $"Invoice {invoice.Id} is {FormatAmount(invoice.Amount)}, which is over the " +
                $"{approver.Role} approval limit of {FormatAmount(limit)}. A Director must approve it.");
        }

        return Finalise(invoice, approver);
    }

    public Invoice Reject(string invoiceId, string approver, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required to reject an invoice.", nameof(reason));
        }

        lock (DecisionLock)
        {
            var invoice = Find(invoiceId);
            EnsureDecidable(invoice);

            invoice.Status = InvoiceStatus.Rejected;
            invoice.DecidedBy = approver;
            invoice.DecidedAt = DateTime.UtcNow;
            invoice.DecisionNote = reason;
            _repository.Update(invoice);
            return invoice;
        }
    }

    /// <summary>First of two approvals: any Manager or Director. The invoice is not approved yet.</summary>
    private Invoice GiveFirstApproval(Invoice invoice, Approver approver)
    {
        // First approver before status: an invoice is never AwaitingSecondApproval without one.
        invoice.FirstApprovedBy = approver.Username;
        invoice.FirstApprovedAt = DateTime.UtcNow;
        invoice.Status = InvoiceStatus.AwaitingSecondApproval;
        _repository.Update(invoice);
        return invoice;
    }

    /// <summary>Second of two approvals: a different person, and a Director. Checks first, then changes.</summary>
    private Invoice GiveSecondApproval(Invoice invoice, Approver approver)
    {
        if (string.IsNullOrWhiteSpace(invoice.FirstApprovedBy))
        {
            // Fail closed: without a recorded first approver we cannot prove two different people approved.
            throw new ApprovalDeniedException(
                $"Invoice {invoice.Id} is awaiting a second approval but has no recorded first approver; " +
                "it cannot be approved until this is corrected.");
        }

        if (IsSamePerson(invoice.FirstApprovedBy, approver.Username))
        {
            throw new ApprovalDeniedException(
                $"{approver.Username} already gave the first approval on invoice {invoice.Id}; " +
                "the second approval must come from a different person.");
        }

        if (approver.Role != DirectorRole)
        {
            throw new ApprovalDeniedException(
                $"The second approval on invoice {invoice.Id} (over {FormatAmount(DirectorLimit)}) " +
                $"must come from a Director; {approver.Username} is a {approver.Role}.");
        }

        return Finalise(invoice, approver);
    }

    private Invoice Finalise(Invoice invoice, Approver approver)
    {
        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approver.Username;
        invoice.DecidedAt = DateTime.UtcNow;
        _repository.Update(invoice);
        return invoice;
    }

    private Invoice Find(string invoiceId) =>
        _repository.GetById(invoiceId)
        ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

    private static void EnsureDecidable(Invoice invoice)
    {
        if (invoice.Status is not (InvoiceStatus.PendingApproval or InvoiceStatus.AwaitingSecondApproval))
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided.");
        }
    }
}
