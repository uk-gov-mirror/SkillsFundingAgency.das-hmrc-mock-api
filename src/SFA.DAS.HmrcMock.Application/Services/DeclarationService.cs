using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using SFA.DAS.HmrcMock.Domain.Interfaces;

namespace SFA.DAS.HmrcMock.Application.Services;

public interface ILevyDeclarationService
{
    Task<LevyDeclarationResponse> GetByEmpRef(string empRef);

    Task CreateDeclarationsAsync(string empref, int numberOfDeclarations, long amount);

    Task<AppendDeclarationResult> AppendDeclarationAsync(AppendDeclarationRequest request);
}

[ExcludeFromCodeCoverage]
public class MongoLevyDeclarationService(IMongoDatabase database, IDateTimeService dateTimeService)
    : BaseMongoService<LevyDeclarationResponse>(database, "declarations"), ILevyDeclarationService
{
    private const long DefaultLevyDueYtd = 1000;
    private const long DefaultLevyAllowanceForFullYear = 15000;

    public async Task<LevyDeclarationResponse> GetByEmpRef(string empref)
    {
        var filter = Builders<LevyDeclarationResponse>.Filter.Eq("empref", empref);
        return await FindOne(filter);
    }

    public async Task CreateDeclarationsAsync(string empref, int numberOfDeclarations, long amount)
    {
        var declarations = new List<DeclarationResponse>();
        long levyDueYtd = 0;
        var now = dateTimeService.GetDateTime();

        for (var i = numberOfDeclarations; i > 0; i--)
        {
            _ = long.TryParse(now.ToString("yssfffffff"), out var declarationId);
            var submissionDate = now.AddMonths(-i);
            levyDueYtd += amount;

            var payrollYear = PayrollPeriodHelper.GetPayrollYear(submissionDate);
            var payrollMonth = PayrollPeriodHelper.GetPayrollMonth(submissionDate);

            declarations.Add(new DeclarationResponse
            {
                DeclarationId = declarationId,
                SubmissionTime = submissionDate,
                LevyDueYTD = levyDueYtd,
                LevyAllowanceForFullYear = 15000,
                PayrollPeriod = new PayrollPeriodResponse
                {
                    Year = payrollYear,
                    Month = payrollMonth,
                }
            });
        }

        var levyDeclarationsDto = new LevyDeclarationResponse
        {
            EmpRef = empref,
            Declarations = declarations
        };

        await CreateOne(levyDeclarationsDto);
    }

    public async Task<AppendDeclarationResult> AppendDeclarationAsync(AppendDeclarationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.EmpRef))
        {
            return new AppendDeclarationResult
            {
                Status = AppendDeclarationStatus.InvalidRequest,
                ErrorMessage = "empRef is required."
            };
        }

        var existing = await GetByEmpRef(request.EmpRef);
        if (existing == null)
        {
            return new AppendDeclarationResult { Status = AppendDeclarationStatus.NotFound };
        }

        var now = dateTimeService.GetDateTime();
        string payrollYear;
        int payrollMonth;

        if (!string.IsNullOrWhiteSpace(request.PayrollYear) || request.PayrollMonth.HasValue)
        {
            if (string.IsNullOrWhiteSpace(request.PayrollYear) || !request.PayrollMonth.HasValue)
            {
                return new AppendDeclarationResult
                {
                    Status = AppendDeclarationStatus.InvalidRequest,
                    ErrorMessage = "payrollYear and payrollMonth must both be supplied when overriding the period."
                };
            }

            payrollYear = request.PayrollYear;
            payrollMonth = request.PayrollMonth.Value;
        }
        else
        {
            (payrollYear, payrollMonth) = PayrollPeriodHelper.GetLatestImportablePeriod(now);
        }

        if (PayrollPeriodHelper.IsFuturePeriod(payrollYear, payrollMonth, now))
        {
            return new AppendDeclarationResult
            {
                Status = AppendDeclarationStatus.FuturePeriod,
                ErrorMessage =
                    $"Payroll period {payrollYear} month {payrollMonth} is still future for Finance import (importable after the 20th of the following calendar month)."
            };
        }

        var submissionTime = request.SubmissionTime
            ?? PayrollPeriodHelper.GetOnTimeSubmissionTime(payrollYear, payrollMonth, now);

        var declaration = new DeclarationResponse
        {
            DeclarationId = CreateDeclarationId(now),
            SubmissionTime = submissionTime,
            LevyDueYTD = request.LevyDueYTD ?? DefaultLevyDueYtd,
            LevyAllowanceForFullYear = request.LevyAllowanceForFullYear ?? DefaultLevyAllowanceForFullYear,
            PayrollPeriod = new PayrollPeriodResponse
            {
                Year = payrollYear,
                Month = payrollMonth
            }
        };

        existing.Declarations ??= [];
        var existingForPeriod = existing.Declarations
            .FirstOrDefault(d => d.PayrollPeriod?.Year == payrollYear && d.PayrollPeriod?.Month == payrollMonth);

        if (existingForPeriod != null)
        {
            existing.Declarations.Remove(existingForPeriod);
        }

        existing.Declarations.Add(declaration);

        var filter = Builders<LevyDeclarationResponse>.Filter.Eq("empref", request.EmpRef);
        await ReplaceOne(filter, existing);

        return new AppendDeclarationResult
        {
            Status = AppendDeclarationStatus.Success,
            Declaration = declaration
        };
    }

    private static long CreateDeclarationId(DateTime now)
    {
        _ = long.TryParse(now.ToString("yssfffffff"), out var declarationId);
        if (declarationId == 0)
        {
            declarationId = now.Ticks;
        }

        return declarationId;
    }
}

[BsonIgnoreExtraElements]
public class LevyDeclarationResponse
{
    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    [BsonElement("_id")]
    public ObjectId Id { get; set; }
    [BsonElement("empref")]
    public string EmpRef { get; set; }
    [BsonElement("declarations")]
    public List<DeclarationResponse> Declarations { get; set; }
}

[BsonIgnoreExtraElements]
public class DeclarationResponse
{
    [BsonElement("id")]
    public long DeclarationId { get; set; }
    [BsonElement("submissionTime")]
    public DateTime SubmissionTime { get; set; }
    [BsonElement("payrollPeriod")]
    public PayrollPeriodResponse PayrollPeriod { get; set; }
    [BsonElement("levyDueYTD")]
    public long LevyDueYTD { get; set; }
    [BsonElement("levyAllowanceForFullYear")]
    public long LevyAllowanceForFullYear { get; set; }

    public long Id => DeclarationId;
}

[BsonIgnoreExtraElements]
public class PayrollPeriodResponse
{
    [BsonElement("year")]
    public string Year { get; set; }
    [BsonElement("month")]
    public int Month { get; set; }
}
