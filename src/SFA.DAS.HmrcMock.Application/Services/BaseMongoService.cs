using System.Diagnostics.CodeAnalysis;
using MongoDB.Driver;

namespace SFA.DAS.HmrcMock.Application.Services;

[ExcludeFromCodeCoverage]
public class BaseMongoService<T>
{
    protected readonly IMongoCollection<T> _collection;

    protected BaseMongoService(IMongoDatabase database, string collectionName)
    {
        _collection = database.GetCollection<T>(collectionName);
    }

    protected async Task<T> FindOne(FilterDefinition<T> filter)
    {
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    protected async Task CreateOne(T entity)
    {
        await _collection.InsertOneAsync(entity);
    }

    protected async Task ReplaceOne(FilterDefinition<T> filter, T entity)
    {
        await _collection.ReplaceOneAsync(filter, entity);
    }
}