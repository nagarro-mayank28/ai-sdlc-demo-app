using InvoiceApp.Api.Auth;
using InvoiceApp.Domain;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequireRole("Manager", "Director", "Admin")]
public class ApprovalsController : ApiControllerBase
{
    private readonly ApprovalService _approvals;

    public ApprovalsController(ApprovalService approvals)
    {
        _approvals = approvals;
    }

    public record RejectRequest(string Reason);

    [HttpPost("{invoiceId}/approve")]
    public ActionResult<Invoice> Approve(string invoiceId) =>
        Handle(() => _approvals.Approve(invoiceId, CurrentApprover()));

    [HttpPost("{invoiceId}/reject")]
    public ActionResult<Invoice> Reject(string invoiceId, [FromBody] RejectRequest request) =>
        Handle(() => _approvals.Reject(invoiceId, HttpContext.GetSession().Username, request.Reason));

    /// <summary>The approver is always the signed-in user; nothing in the request can change who or what role it is.</summary>
    private Approver CurrentApprover()
    {
        var session = HttpContext.GetSession();
        return new Approver(session.Username, session.Role);
    }
}
