using MongoDB.Driver;

public sealed class TravelHistoryRepository
{
    private readonly IMongoCollection<TravelSessionRecord> _collection;

    public TravelHistoryRepository(IConfiguration configuration)
    {
        var settings = configuration
            .GetSection("MongoDb")
            .Get<MongoDbSettings>()
            ?? throw new InvalidOperationException("MongoDb configuration missing.");

        var client = new MongoClient(settings.ConnectionString);

        var database = client.GetDatabase(settings.DatabaseName);

        _collection = database.GetCollection<TravelSessionRecord>(settings.CollectionName);
    }

    public async Task InsertAsync(TravelSessionRecord record, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(
        record,
        cancellationToken: cancellationToken);

        await DeleteOldRecordsAsync(cancellationToken);
    }

    public async Task DeleteOldRecordsAsync(CancellationToken cancellationToken = default)
    {
        var records = await _collection
            .Find(FilterDefinition<TravelSessionRecord>.Empty)
            .SortByDescending(x => x.CompletedAt)
            .ToListAsync(cancellationToken);

        if (records.Count <= 20)
            return;

        var idsToDelete = records
            .Skip(20)
            .Select(x => x.Id)
            .ToList();

        var filter = Builders<TravelSessionRecord>.Filter.In(x => x.Id, idsToDelete);

        await _collection.DeleteManyAsync(filter, cancellationToken);
    }
}