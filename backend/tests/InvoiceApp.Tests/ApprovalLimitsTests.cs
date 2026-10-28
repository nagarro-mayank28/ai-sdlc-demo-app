using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalLimitsTests
{
    [Fact]
    public void ManagerLimit_IsFiftyThousand()
    {
        Assert.Equal(50_000.00m, ApprovalLimits.SingleApproverLimitFor("Manager"));
    }

    [Fact]
    public void DirectorLimit_IsFiveHundredThousand()
    {
        Assert.Equal(500_000.00m, ApprovalLimits.SingleApproverLimitFor("Director"));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Approver")]
    [InlineData("")]
    [InlineData(null)]
    public void RolesOtherThanManagerOrDirector_HaveNoLimit(string? role)
    {
        Assert.Null(ApprovalLimits.SingleApproverLimitFor(role));
    }

    [Fact]
    public void FormatAmount_UsesTwoDecimalsAndThousandSeparators()
    {
        Assert.Equal("1,234,567.50", ApprovalLimits.FormatAmount(1_234_567.5m));
    }
}
