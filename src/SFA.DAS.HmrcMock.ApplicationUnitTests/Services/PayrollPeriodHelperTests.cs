using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.HmrcMock.Application.Services;

namespace SFA.DAS.HmrcMock.Application.UnitTests;

[TestFixture]
public class PayrollPeriodHelperTests
{
    [Test]
    public void GetLatestImportablePeriod_Before20th_SkipsPreviousCalendarMonth()
    {
        // 10 Aug: Jul period start 20 Jul + 1m = 20 Aug > 10 Aug → future; June is importable.
        var now = new DateTime(2026, 8, 10);

        var (year, month) = PayrollPeriodHelper.GetLatestImportablePeriod(now);

        year.Should().Be("26-27");
        month.Should().Be(PayrollPeriodHelper.GetPayrollMonth(new DateTime(2026, 6, 1)));
        PayrollPeriodHelper.IsFuturePeriod(year, month, now).Should().BeFalse();
    }

    [Test]
    public void GetLatestImportablePeriod_OnOrAfter20th_AllowsPreviousCalendarMonth()
    {
        // 21 Aug: Jul period start 20 Jul + 1m = 20 Aug <= 21 Aug → importable.
        var now = new DateTime(2026, 8, 21);

        var (year, month) = PayrollPeriodHelper.GetLatestImportablePeriod(now);

        year.Should().Be("26-27");
        month.Should().Be(PayrollPeriodHelper.GetPayrollMonth(new DateTime(2026, 7, 1)));
        PayrollPeriodHelper.IsFuturePeriod(year, month, now).Should().BeFalse();
    }

    [Test]
    public void IsFuturePeriod_CurrentCalendarMonth_IsFutureMidMonth()
    {
        var now = new DateTime(2026, 8, 10);
        var year = PayrollPeriodHelper.GetPayrollYear(now);
        var month = PayrollPeriodHelper.GetPayrollMonth(now);

        PayrollPeriodHelper.IsFuturePeriod(year, month, now).Should().BeTrue();
    }

    [Test]
    public void GetOnTimeSubmissionTime_UsesPeriodStart_WhenOutsideWindow()
    {
        var now = new DateTime(2026, 8, 21);
        var year = PayrollPeriodHelper.GetPayrollYear(new DateTime(2026, 7, 1));
        var month = PayrollPeriodHelper.GetPayrollMonth(new DateTime(2026, 7, 1));

        var submissionTime = PayrollPeriodHelper.GetOnTimeSubmissionTime(year, month, now);

        submissionTime.Should().Be(PayrollPeriodHelper.GetPeriodStart(year, month));
    }
}
