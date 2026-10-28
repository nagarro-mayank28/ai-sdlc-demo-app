namespace InvoiceApp.Domain;

/// <summary>
/// An approval the rules refuse (over the approver's limit, wrong role, same person twice).
/// The message says why in words a finance user can act on. The invoice is left unchanged.
/// </summary>
public class ApprovalDeniedException : Exception
{
    public ApprovalDeniedException(string message) : base(message)
    {
    }
}
