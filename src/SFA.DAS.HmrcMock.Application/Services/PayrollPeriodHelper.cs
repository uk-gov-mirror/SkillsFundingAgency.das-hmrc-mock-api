namespace SFA.DAS.HmrcMock.Application.Services;

public static class PayrollPeriodHelper
{
    public static string GetPayrollYear(DateTime calendarDate)
    {
        var startYear = calendarDate.Month < 4 ? calendarDate.Year - 1 : calendarDate.Year;
        var endYear = startYear + 1;
        return $"{startYear % 100:00}-{endYear % 100:00}";
    }

    public static int GetPayrollMonth(DateTime calendarDate)
    {
        return calendarDate.Month >= 4 ? calendarDate.Month - 3 : calendarDate.Month + 9;
    }

    // Matches Employer Finance HmrcDateService.GetDateFromPayrollYearMonth (20th of calendar month).
    public static DateTime GetPeriodStart(string payrollYear, int payrollMonth)
    {
        var yearSplit = payrollYear.Split('-');
        int yearToUse;
        int monthToUse;

        if (payrollMonth >= 10)
        {
            yearToUse = 2000 + Convert.ToInt32(yearSplit[1]);
            monthToUse = payrollMonth - 9;
        }
        else
        {
            yearToUse = 2000 + Convert.ToInt32(yearSplit[0]);
            monthToUse = payrollMonth + 3;
        }

        return new DateTime(yearToUse, monthToUse, 20);
    }

    // Matches Employer Finance HmrcDateService.IsSubmissionForFuturePeriod.
    public static bool IsFuturePeriod(string payrollYear, int payrollMonth, DateTime now)
    {
        return GetPeriodStart(payrollYear, payrollMonth).AddMonths(1) > now;
    }

    public static (string PayrollYear, int PayrollMonth) GetLatestImportablePeriod(DateTime now)
    {
        var candidate = new DateTime(now.Year, now.Month, 1);

        for (var i = 0; i < 24; i++)
        {
            var payrollYear = GetPayrollYear(candidate);
            var payrollMonth = GetPayrollMonth(candidate);

            if (!IsFuturePeriod(payrollYear, payrollMonth, now))
            {
                return (payrollYear, payrollMonth);
            }

            candidate = candidate.AddMonths(-1);
        }

        throw new InvalidOperationException("Could not find an importable payroll period within the last 24 months.");
    }

    public static DateTime GetOnTimeSubmissionTime(string payrollYear, int payrollMonth, DateTime now)
    {
        var periodStart = GetPeriodStart(payrollYear, payrollMonth);
        var periodEnd = periodStart.AddMonths(1).AddMilliseconds(-1);

        if (now >= periodStart && now <= periodEnd)
        {
            return now;
        }

        return periodStart;
    }
}
