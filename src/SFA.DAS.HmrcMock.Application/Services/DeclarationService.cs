using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace SFA.DAS.HmrcMock.Application.Services;

public interface ILevyDeclarationService
{
    Task<LevyDeclarationResponse> GetByEmpRef(string empRef);
    
    Task CreateDeclarationsAsync(string empref, int numberOfDeclarations, long amount);
}

[ExcludeFromCodeCoverage]
public class MongoLevyDeclarationService(IMongoDatabase database) : BaseMongoService<LevyDeclarationResponse>(database, "declarations"), ILevyDeclarationService
{
    public async Task<LevyDeclarationResponse> GetByEmpRef(string empref)
    {
        var filter = Builders<LevyDeclarationResponse>.Filter.Eq("empref", empref);
        return await FindOne(filter);
    }

    public async Task CreateDeclarationsAsync(string empref, int numberOfDeclarations, long amount)
    {
        var declarations = PayrollPeriodHelper.BuildHistoricalDeclarations(DateTime.Now, numberOfDeclarations, amount)
            .Select(seed => new DeclarationResponse
            {
                DeclarationId = seed.DeclarationId,
                SubmissionTime = seed.SubmissionDate,
                LevyDueYTD = seed.LevyDueYtd,
                LevyAllowanceForFullYear = 15000,
                PayrollPeriod = new PayrollPeriodResponse
                {
                    Year = seed.PayrollYear,
                    Month = seed.PayrollMonth,
                }
            })
            .ToList();

        var levyDeclarationsDto = new LevyDeclarationResponse
        {
            EmpRef = empref,
            Declarations = declarations
        };

        await CreateOne(levyDeclarationsDto);
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