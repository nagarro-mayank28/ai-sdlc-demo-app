using InvoiceApp.Domain;

namespace InvoiceApp.Tests;

/// <summary>
/// Every field of an invoice, captured by value. Two snapshots are equal only if nothing changed,
/// which is how tests prove a refused approval left the invoice alone.
/// </summary>
internal sealed record InvoiceSnapshot(
    string Id,
    string CustomerName,
    decimal Amount,
    InvoiceStatus Status,
    DateTime CreatedAt,
    string? DecidedBy,
    DateTime? DecidedAt,
    string? DecisionNote)
{
    public static InvoiceSnapshot Of(Invoice invoice) => new(
        invoice.Id,
        invoice.CustomerName,
        invoice.Amount,
        invoice.Status,
        invoice.CreatedAt,
        invoice.DecidedBy,
        invoice.DecidedAt,
        invoice.DecisionNote);
}
