using System.Globalization;
using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private static readonly Approver Manager = new("demo.manager", "Manager");
    private static readonly Approver Director = new("demo.director", "Director");
    private static readonly Approver SecondDirector = new("demo.director2", "Director");
    private static readonly Approver Admin = new("demo.admin", "Admin");

    private static Invoice InvoiceWith(string id, InvoiceStatus status, decimal amount = 100m) =>
        new() { Id = id, CustomerName = "Test School", Amount = amount, Status = status };

    private static Invoice PendingInvoice(decimal amount) =>
        InvoiceWith("INV-1", InvoiceStatus.PendingApproval, amount);

    private static decimal Amount(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    private static ApprovalService ServiceFor(params Invoice[] invoices) =>
        new(new FakeInvoiceRepository(invoices));

    /// <summary>
    /// Runs an approval that must be refused and proves the refusal changed nothing:
    /// the invoice has the same value in every field and nothing was written back.
    /// </summary>
    private static ApprovalDeniedException AssertDeniedAndUnchanged(
        ApprovalService service, FakeInvoiceRepository repository, Invoice invoice, Approver approver)
    {
        var before = InvoiceSnapshot.Of(invoice);
        var updatesBefore = repository.UpdateCount;

        var denial = Assert.Throws<ApprovalDeniedException>(() => service.Approve(invoice.Id, approver));

        Assert.Equal(before, InvoiceSnapshot.Of(invoice));
        Assert.Equal(updatesBefore, repository.UpdateCount);
        return denial;
    }

    private static ApprovalDeniedException AssertDeniedAndUnchanged(Invoice invoice, Approver approver)
    {
        var repository = new FakeInvoiceRepository(invoice);
        return AssertDeniedAndUnchanged(new ApprovalService(repository), repository, invoice, approver);
    }

    /// <summary>A 750,000.00 invoice that has had its first approval from <paramref name="firstApprover"/>.</summary>
    private static (ApprovalService Service, FakeInvoiceRepository Repository, Invoice Invoice) AwaitingSecondApprovalFrom(
        Approver firstApprover)
    {
        var invoice = PendingInvoice(Amount("750000.00"));
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        service.Approve(invoice.Id, firstApprover);

        Assert.Equal(InvoiceStatus.AwaitingSecondApproval, invoice.Status);
        return (service, repository, invoice);
    }

    // ---- Lifecycle (unchanged behaviour) ----

    [Fact]
    public void Submit_MovesDraftToPendingApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);

        var result = ServiceFor(invoice).Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
    }

    [Fact]
    public void Submit_RejectsInvoiceThatIsNotADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));
    }

    [Fact]
    public void Approve_SetsStatusApproverAndTimestamp()
    {
        var repository = new FakeInvoiceRepository(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));
        var service = new ApprovalService(repository);

        var result = service.Approve("INV-1", Manager);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsStillADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Draft));

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", Manager));
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsAlreadyApproved()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", Director));
    }

    [Fact]
    public void Approve_ThrowsWhenInvoiceDoesNotExist()
    {
        var service = ServiceFor();

        Assert.Throws<KeyNotFoundException>(() => service.Approve("INV-404", Manager));
    }

    [Fact]
    public void Reject_RecordsReasonAndApprover()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Reject("INV-1", "demo.manager", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("Duplicate invoice", result.DecisionNote);
        Assert.Equal("demo.manager", result.DecidedBy);
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        Assert.Throws<ArgumentException>(() => service.Reject("INV-1", "demo.manager", "  "));
    }

    [Fact]
    public void Reject_HasNoAmountLimit()
    {
        var service = ServiceFor(PendingInvoice(Amount("9000000.00")));

        var result = service.Reject("INV-1", "demo.manager", "Wrong supplier");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
    }

    // ---- Rule: a Manager may approve up to 50,000 ----

    [Theory]
    [InlineData("0.01")]
    [InlineData("49999.99")]
    [InlineData("50000.00")]
    public void Manager_ApprovesInvoicesUpToAndIncludingFiftyThousand(string amount)
    {
        var service = ServiceFor(PendingInvoice(Amount(amount)));

        var result = service.Approve("INV-1", Manager);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.DecidedBy);
    }

    [Theory]
    [InlineData("50000.01")]
    [InlineData("75000.00")]
    [InlineData("500000.00")]
    public void Manager_IsRefusedOverFiftyThousand_AndInvoiceIsUnchanged(string amount)
    {
        var denial = AssertDeniedAndUnchanged(PendingInvoice(Amount(amount)), Manager);

        Assert.Contains("Manager approval limit of 50,000.00", denial.Message);
        Assert.Contains("A Director must approve it", denial.Message);
    }

    [Fact]
    public void Manager_RefusalNamesTheInvoiceAndItsAmount()
    {
        var denial = AssertDeniedAndUnchanged(PendingInvoice(Amount("75000.00")), Manager);

        Assert.Contains("INV-1", denial.Message);
        Assert.Contains("75,000.00", denial.Message);
    }

    // ---- Rule: a Director may approve up to 500,000 ----

    [Theory]
    [InlineData("50000.01")]
    [InlineData("250000.00")]
    [InlineData("500000.00")]
    public void Director_ApprovesInvoicesUpToAndIncludingFiveHundredThousand(string amount)
    {
        var service = ServiceFor(PendingInvoice(Amount(amount)));

        var result = service.Approve("INV-1", Director);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director", result.DecidedBy);
    }

    // ---- Rule: roles without approval authority ----

    [Fact]
    public void Admin_CannotApprove_AndInvoiceIsUnchanged()
    {
        var denial = AssertDeniedAndUnchanged(PendingInvoice(Amount("10.00")), Admin);

        Assert.Contains("Admin has no approval authority", denial.Message);
        Assert.Contains("INV-1", denial.Message);
        Assert.Contains("Only a Manager or a Director can approve", denial.Message);
    }

    [Fact]
    public void RetiredApproverRole_CannotApprove()
    {
        var denial = AssertDeniedAndUnchanged(PendingInvoice(Amount("10.00")), new Approver("old.user", "Approver"));

        Assert.Contains("no approval authority", denial.Message);
    }

    [Fact]
    public void Approve_RequiresAnApprover()
    {
        var service = ServiceFor(PendingInvoice(Amount("10.00")));

        Assert.Throws<ArgumentNullException>(() => service.Approve("INV-1", null!));
    }

    // ---- Rule: above 500,000 two distinct approvers, the second a Director ----

    [Fact]
    public void AboveFiveHundredThousand_DirectorGivesOnlyTheFirstApproval()
    {
        var repository = new FakeInvoiceRepository(PendingInvoice(Amount("500000.01")));
        var service = new ApprovalService(repository);

        var result = service.Approve("INV-1", Director);

        Assert.Equal(InvoiceStatus.AwaitingSecondApproval, result.Status);
        Assert.Equal("demo.director", result.FirstApprovedBy);
        Assert.NotNull(result.FirstApprovedAt);
        Assert.Null(result.DecidedBy);
        Assert.Null(result.DecidedAt);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void AboveFiveHundredThousand_ManagerMayGiveTheFirstApproval()
    {
        var (_, _, invoice) = AwaitingSecondApprovalFrom(Manager);

        Assert.Equal("demo.manager", invoice.FirstApprovedBy);
        Assert.Null(invoice.DecidedBy);
    }

    [Fact]
    public void AboveFiveHundredThousand_AdminCannotGiveTheFirstApproval()
    {
        var denial = AssertDeniedAndUnchanged(PendingInvoice(Amount("750000.00")), Admin);

        Assert.Contains("no approval authority", denial.Message);
    }

    [Fact]
    public void SecondApprovalByADifferentDirector_ApprovesTheInvoice()
    {
        var (service, _, invoice) = AwaitingSecondApprovalFrom(Director);

        var result = service.Approve(invoice.Id, SecondDirector);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director2", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.Equal("demo.director", result.FirstApprovedBy);
    }

    [Fact]
    public void ManagerFirst_ThenDirectorSecond_ApprovesTheInvoice()
    {
        var (service, _, invoice) = AwaitingSecondApprovalFrom(Manager);

        var result = service.Approve(invoice.Id, Director);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.FirstApprovedBy);
        Assert.Equal("demo.director", result.DecidedBy);
    }

    // Negative rule 1: the same user may never count as both approvers.
    [Theory]
    [InlineData("demo.director")]
    [InlineData("Demo.Director")]
    [InlineData("DEMO.DIRECTOR")]
    [InlineData("  demo.director ")]
    public void SameDirectorTwice_IsRefused_AndInvoiceIsUnchanged(string sameUserSpelledDifferently)
    {
        var (service, repository, invoice) = AwaitingSecondApprovalFrom(Director);

        var denial = AssertDeniedAndUnchanged(
            service, repository, invoice, new Approver(sameUserSpelledDifferently, "Director"));

        Assert.Contains("already gave the first approval on invoice INV-1", denial.Message);
        Assert.Contains(sameUserSpelledDifferently, denial.Message);
        Assert.Contains("different person", denial.Message);
        Assert.Equal(InvoiceStatus.AwaitingSecondApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
    }

    // Negative rule 2: the second approver must be a Director.
    [Fact]
    public void ManagerAsSecondApprover_IsRefused_AndFirstApprovalIsKept()
    {
        var (service, repository, invoice) = AwaitingSecondApprovalFrom(Director);

        var denial = AssertDeniedAndUnchanged(service, repository, invoice, Manager);

        Assert.Contains("The second approval on invoice INV-1 (over 500,000.00)", denial.Message);
        Assert.Contains("must come from a Director", denial.Message);
        Assert.Contains("demo.manager is a Manager", denial.Message);
        Assert.Equal("demo.director", invoice.FirstApprovedBy);
        Assert.Equal(InvoiceStatus.AwaitingSecondApproval, invoice.Status);
    }

    [Fact]
    public void TwoManagers_CannotCompleteATwoApproverInvoice()
    {
        var (service, repository, invoice) = AwaitingSecondApprovalFrom(Manager);

        var denial = AssertDeniedAndUnchanged(service, repository, invoice, new Approver("other.manager", "Manager"));

        Assert.Contains("must come from a Director", denial.Message);
    }

    [Fact]
    public void ManagerWhoGaveTheFirstApproval_CannotGiveTheSecond()
    {
        var (service, repository, invoice) = AwaitingSecondApprovalFrom(Manager);

        var denial = AssertDeniedAndUnchanged(service, repository, invoice, Manager);

        Assert.Contains("different person", denial.Message);
    }

    [Fact]
    public void AdminAsSecondApprover_IsRefused()
    {
        var (service, repository, invoice) = AwaitingSecondApprovalFrom(Director);

        var denial = AssertDeniedAndUnchanged(service, repository, invoice, Admin);

        Assert.Contains("no approval authority", denial.Message);
    }

    [Fact]
    public void AfterBothApprovals_AThirdApprovalIsAConflict()
    {
        var (service, _, invoice) = AwaitingSecondApprovalFrom(Director);
        service.Approve(invoice.Id, SecondDirector);

        Assert.Throws<InvalidOperationException>(() => service.Approve(invoice.Id, Director));
    }

    [Fact]
    public void AwaitingSecondApproval_CanBeRejected()
    {
        var (service, _, invoice) = AwaitingSecondApprovalFrom(Director);

        var result = service.Reject(invoice.Id, "demo.director2", "Budget not signed off");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("Budget not signed off", result.DecisionNote);
    }

    [Fact]
    public void FirstApproverStoredWithDifferentCaseAndSpaces_StillCountsAsTheSamePerson()
    {
        var (service, repository, invoice) = AwaitingSecondApprovalFrom(new Approver(" Demo.Director ", "Director"));

        var denial = AssertDeniedAndUnchanged(service, repository, invoice, Director);

        Assert.Contains("different person", denial.Message);
    }

    [Fact]
    public void AwaitingSecondApprovalWithNoRecordedFirstApprover_FailsClosed()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.AwaitingSecondApproval, Amount("750000.00"));

        var denial = AssertDeniedAndUnchanged(invoice, Director);

        Assert.Contains("no recorded first approver", denial.Message);
    }
}
