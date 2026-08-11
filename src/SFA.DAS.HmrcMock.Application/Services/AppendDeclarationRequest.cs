namespace SFA.DAS.HmrcMock.Application.Services;

public class AppendDeclarationRequest
{
    public string EmpRef { get; set; } = string.Empty;
    public long? LevyDueYTD { get; set; }
    public long? LevyAllowanceForFullYear { get; set; }
    public string? PayrollYear { get; set; }
    public int? PayrollMonth { get; set; }
    public DateTime? SubmissionTime { get; set; }
}

public enum AppendDeclarationStatus
{
    Success,
    NotFound,
    FuturePeriod,
    InvalidRequest
}

public class AppendDeclarationResult
{
    public AppendDeclarationStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DeclarationResponse? Declaration { get; set; }
}
