using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.HmrcMock.Application.Services;

namespace SFA.DAS.HmrcMock.Application.UnitTests;

[TestFixture]
public class PayrollPeriodHelperTests
{
    [Test]
    public void BuildHistoricalDeclarations_AccumulatesYtdWithinAPayrollYear()
    {
        var now = new DateTime(2026, 9, 26);
        const long amount = 9999;

        var declarations = PayrollPeriodHelper.BuildHistoricalDeclarations(now, 3, amount);

        declarations.Should().HaveCount(3);
        declarations[0].PayrollMonth.Should().Be(3);
        declarations[0].LevyDueYtd.Should().Be(amount);
        declarations[1].LevyDueYtd.Should().Be(amount * 2);
        declarations[2].LevyDueYtd.Should().Be(amount * 3);
    }

    [Test]
    public void BuildHistoricalDeclarations_ResetsYtdInApril()
    {
        var now = new DateTime(2026, 5, 26);
        const long amount = 9999;

        var declarations = PayrollPeriodHelper.BuildHistoricalDeclarations(now, 2, amount);

        var march = declarations.Single(d => d.SubmissionDate.Month == 3);
        var april = declarations.Single(d => d.SubmissionDate.Month == 4);

        march.PayrollYear.Should().Be("25-26");
        march.PayrollMonth.Should().Be(12);
        march.LevyDueYtd.Should().Be(amount);

        april.PayrollYear.Should().Be("26-27");
        april.PayrollMonth.Should().Be(1);
        april.LevyDueYtd.Should().Be(amount);
    }

    [Test]
    public void BuildHistoricalDeclarations_Le36_DoesNotRollPriorYearsIntoApril()
    {
        var now = new DateTime(2026, 8, 26);
        const long amount = 9999;

        var declarations = PayrollPeriodHelper.BuildHistoricalDeclarations(now, 36, amount);

        var april2026 = declarations.Single(d => d.PayrollYear == "26-27" && d.PayrollMonth == 1);
        var march2026 = declarations.Single(d => d.PayrollYear == "25-26" && d.PayrollMonth == 12);

        april2026.LevyDueYtd.Should().Be(amount);
        march2026.LevyDueYtd.Should().Be(amount * 12);
        april2026.LevyDueYtd.Should().NotBe(amount * 33);
    }

    [Test]
    public void BuildHistoricalDeclarations_UsesUniqueDeclarationIds()
    {
        var now = new DateTime(2026, 8, 26);

        var declarations = PayrollPeriodHelper.BuildHistoricalDeclarations(now, 36, 9999);

        declarations.Select(d => d.DeclarationId).Should().OnlyHaveUniqueItems();
    }
}
