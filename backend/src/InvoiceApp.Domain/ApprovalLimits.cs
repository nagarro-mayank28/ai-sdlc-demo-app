using System.Globalization;

namespace InvoiceApp.Domain;

/// <summary>
/// Who may approve how much. The only place approval thresholds are defined.
/// Limits are inclusive: a Manager may approve exactly 50,000.00.
/// </summary>
public static class ApprovalLimits
{
    public const string ManagerRole = "Manager";
    public const string DirectorRole = "Director";

    public const decimal ManagerLimit = 50_000.00m;
    public const decimal DirectorLimit = 500_000.00m;

    /// <summary>The largest amount this role may approve on its own, or null if the role cannot approve.</summary>
    public static decimal? SingleApproverLimitFor(string? role) => role switch
    {
        ManagerRole => ManagerLimit,
        DirectorRole => DirectorLimit,
        _ => null
    };

    /// <summary>Above the director limit one person can never approve alone.</summary>
    public static bool RequiresTwoApprovers(decimal amount) => amount > DirectorLimit;

    /// <summary>
    /// True when two usernames are the same person: compared trimmed and ignoring case,
    /// so "demo.director" and " Demo.Director" cannot count as two approvers.
    /// </summary>
    public static bool IsSamePerson(string? firstUsername, string? secondUsername) =>
        firstUsername is not null
        && secondUsername is not null
        && string.Equals(firstUsername.Trim(), secondUsername.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Formats money the same way in every message, for example 50,000.00.</summary>
    public static string FormatAmount(decimal amount) =>
        amount.ToString("N2", CultureInfo.InvariantCulture);
}
