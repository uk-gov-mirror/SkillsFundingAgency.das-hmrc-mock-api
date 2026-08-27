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

    public static IReadOnlyList<HistoricalLevyDeclaration> BuildHistoricalDeclarations(
        DateTime now,
        int numberOfDeclarations,
        long amount)
    {
        var declarations = new List<HistoricalLevyDeclaration>(numberOfDeclarations);
        long levyDueYtd = 0;

        for (var i = numberOfDeclarations; i > 0; i--)
        {
            var submissionDate = now.AddMonths(-i);
            var payrollYear = GetPayrollYear(submissionDate);
            var payrollMonth = GetPayrollMonth(submissionDate);
            levyDueYtd = payrollMonth == 1 ? amount : levyDueYtd + amount;

            declarations.Add(new HistoricalLevyDeclaration(
                submissionDate,
                payrollYear,
                payrollMonth,
                levyDueYtd,
                submissionDate.Ticks));
        }

        return declarations;
    }
}

public sealed record HistoricalLevyDeclaration(
    DateTime SubmissionDate,
    string PayrollYear,
    int PayrollMonth,
    long LevyDueYtd,
    long DeclarationId);
