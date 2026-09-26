using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MongoDB.Driver;
using SearchDB.Infrastructure.Mongo.Documents;
using SearchDB.Infrastructure.Mongo.Search;
using SearchDB.Infrastructure.Mongo.Seed;
using Xunit;

namespace SearchDB.Infrastructure.Mongo.Tests;

public sealed class AtlasLocalContainerFixture : IAsyncLifetime
{
    private readonly IContainer _container = new ContainerBuilder("mongodb/mongodb-atlas-local:8.3.2")
        .WithEnvironment("DO_NOT_TRACK", "1")
        .WithPortBinding(27017, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilContainerIsHealthy())
        .Build();

    public IMongoDatabase Database { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var port = _container.GetMappedPublicPort(27017);
        var client = new MongoClient($"mongodb://127.0.0.1:{port}/?directConnection=true");
        Database = client.GetDatabase("searchdb_tests");
        await MongoSeed.EnsureSeededAsync(Database);
        await MongoSearchIndexes.EnsureCreatedAsync(Database);
        await WaitForSearchIndex();
    }

    private async Task WaitForSearchIndex()
    {
        var products = Database.GetCollection<ProductDocument>(MongoSearchCollections.Products);
        var pipeline = new[]
        {
            new MongoDB.Bson.BsonDocument("$search", new MongoDB.Bson.BsonDocument
            {
                { "index", MongoSearchIndexes.ProductsIndex },
                { "text", new MongoDB.Bson.BsonDocument { { "query", "keyboard" }, { "path", new MongoDB.Bson.BsonArray { "name" } } } }
            }),
            new MongoDB.Bson.BsonDocument("$limit", 1)
        };
        var expires = DateTimeOffset.UtcNow.AddSeconds(90);
        while (DateTimeOffset.UtcNow < expires)
        {
            try
            {
                if (await products.Aggregate<MongoDB.Bson.BsonDocument>(pipeline).AnyAsync()) return;
            }
            catch (MongoCommandException)
            {
                // Atlas Local accepts index creation before mongot finishes building it.
            }
            await Task.Delay(500);
        }
        throw new TimeoutException("MongoDB Atlas Local Search index did not become queryable within 90 seconds.");
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition("atlas-local")]
public sealed class AtlasLocalCollection : ICollectionFixture<AtlasLocalContainerFixture>
{
}
