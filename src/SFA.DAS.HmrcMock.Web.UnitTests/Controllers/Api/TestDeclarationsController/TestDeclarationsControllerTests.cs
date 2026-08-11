using AutoFixture.NUnit3;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SFA.DAS.HmrcMock.Application.Services;
using SFA.DAS.HmrcMock.Web.Controllers.API;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.HmrcMock.Web.UnitTests.Controllers.Api;

public class TestDeclarationsControllerTests
{
    [Test, MoqAutoData]
    public async Task AppendDeclaration_WhenEmpRefMissing_ReturnsBadRequest(
        [Greedy] TestDeclarationsController controller)
    {
        var result = await controller.AppendDeclaration(new AppendTestDeclarationRequest());

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test, MoqAutoData]
    public async Task AppendDeclaration_WhenNotFound_ReturnsNotFound(
        [Frozen] Mock<ILevyDeclarationService> levyDeclarationService,
        [Greedy] TestDeclarationsController controller)
    {
        levyDeclarationService
            .Setup(x => x.AppendDeclarationAsync(It.IsAny<AppendDeclarationRequest>()))
            .ReturnsAsync(new AppendDeclarationResult { Status = AppendDeclarationStatus.NotFound });

        var result = await controller.AppendDeclaration(new AppendTestDeclarationRequest
        {
            EmpRef = "123/ABC",
            LevyDueYTD = 1000,
            LevyAllowanceForFullYear = 15000
        });

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test, MoqAutoData]
    public async Task AppendDeclaration_WhenFuturePeriod_ReturnsBadRequest(
        [Frozen] Mock<ILevyDeclarationService> levyDeclarationService,
        [Greedy] TestDeclarationsController controller)
    {
        levyDeclarationService
            .Setup(x => x.AppendDeclarationAsync(It.IsAny<AppendDeclarationRequest>()))
            .ReturnsAsync(new AppendDeclarationResult
            {
                Status = AppendDeclarationStatus.FuturePeriod,
                ErrorMessage = "future"
            });

        var result = await controller.AppendDeclaration(new AppendTestDeclarationRequest
        {
            EmpRef = "123/ABC",
            PayrollYear = "26-27",
            PayrollMonth = 5
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test, MoqAutoData]
    public async Task AppendDeclaration_WhenSuccess_ReturnsOkWithDeclaration(
        [Frozen] Mock<ILevyDeclarationService> levyDeclarationService,
        [Greedy] TestDeclarationsController controller)
    {
        var declaration = new DeclarationResponse
        {
            DeclarationId = 42,
            LevyDueYTD = 1000,
            LevyAllowanceForFullYear = 15000,
            SubmissionTime = new DateTime(2026, 6, 20),
            PayrollPeriod = new PayrollPeriodResponse { Year = "26-27", Month = 3 }
        };

        levyDeclarationService
            .Setup(x => x.AppendDeclarationAsync(It.Is<AppendDeclarationRequest>(r =>
                r.EmpRef == "123/ABC" &&
                r.LevyDueYTD == 1000 &&
                r.LevyAllowanceForFullYear == 15000)))
            .ReturnsAsync(new AppendDeclarationResult
            {
                Status = AppendDeclarationStatus.Success,
                Declaration = declaration
            });

        var result = await controller.AppendDeclaration(new AppendTestDeclarationRequest
        {
            EmpRef = "123/ABC",
            LevyDueYTD = 1000,
            LevyAllowanceForFullYear = 15000
        });

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }
}
