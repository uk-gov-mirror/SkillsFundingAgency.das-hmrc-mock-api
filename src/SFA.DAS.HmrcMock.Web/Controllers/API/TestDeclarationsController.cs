using Microsoft.AspNetCore.Mvc;
using SFA.DAS.HmrcMock.Application.Services;

namespace SFA.DAS.HmrcMock.Web.Controllers.API;

[Route("api/test")]
[ApiController]
public class TestDeclarationsController(ILevyDeclarationService levyDeclarationService) : ControllerBase
{
    [HttpPost("declarations")]
    public async Task<IActionResult> AppendDeclaration([FromBody] AppendTestDeclarationRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.EmpRef))
        {
            return BadRequest(new { message = "empRef is required in the request body." });
        }

        var result = await levyDeclarationService.AppendDeclarationAsync(new AppendDeclarationRequest
        {
            EmpRef = request.EmpRef.Trim(),
            LevyDueYTD = request.LevyDueYTD,
            LevyAllowanceForFullYear = request.LevyAllowanceForFullYear,
            PayrollYear = request.PayrollYear,
            PayrollMonth = request.PayrollMonth,
            SubmissionTime = request.SubmissionTime
        });

        return result.Status switch
        {
            AppendDeclarationStatus.Success => Ok(new
            {
                empRef = request.EmpRef.Trim(),
                declaration = result.Declaration
            }),
            AppendDeclarationStatus.NotFound => NotFound(new
            {
                message = $"No declarations document found for empRef '{request.EmpRef.Trim()}'."
            }),
            AppendDeclarationStatus.FuturePeriod => BadRequest(new { message = result.ErrorMessage }),
            AppendDeclarationStatus.InvalidRequest => BadRequest(new { message = result.ErrorMessage }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}

public class AppendTestDeclarationRequest
{
    public string EmpRef { get; set; } = string.Empty;
    public long? LevyDueYTD { get; set; }
    public long? LevyAllowanceForFullYear { get; set; }
    public string? PayrollYear { get; set; }
    public int? PayrollMonth { get; set; }
    public DateTime? SubmissionTime { get; set; }
}
