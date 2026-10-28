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

    [Theory]
    [InlineData("500000.00", false)]
    [InlineData("500000.01", true)]
    [InlineData("10.00", false)]
    public void RequiresTwoApprovers_OnlyAboveFiveHundredThousand(string amount, bool expected)
    {
        Assert.Equal(expected, ApprovalLimits.RequiresTwoApprovers(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("demo.director", "demo.director", true)]
    [InlineData("demo.director", "Demo.Director", true)]
    [InlineData("demo.director", " demo.director  ", true)]
    [InlineData("demo.director", "demo.director2", false)]
    [InlineData(null, "demo.director", false)]
    [InlineData("demo.director", null, false)]
    public void IsSamePerson_IgnoresCaseAndSurroundingSpaces(string? first, string? second, bool expected)
    {
        Assert.Equal(expected, ApprovalLimits.IsSamePerson(first, second));
    }
}
