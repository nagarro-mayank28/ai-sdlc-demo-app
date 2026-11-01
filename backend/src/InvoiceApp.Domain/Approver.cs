namespace InvoiceApp.Domain;

/// <summary>The signed-in person trying to approve: their username and role, taken from the session.</summary>
public record Approver(string Username, string Role);
