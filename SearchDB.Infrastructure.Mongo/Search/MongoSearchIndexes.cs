using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SearchDB.Infrastructure.Mongo.Search;

public static class MongoSearchIndexes
{
    public const string ProductsIndex = "products-search";
    public const string OrdersIndex = "orders-search";

    public static async Task EnsureCreatedAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        await CreateIndex(database.GetCollection<BsonDocument>("products"), ProductsIndex, "products-search-index.json", cancellationToken);
        await CreateIndex(database.GetCollection<BsonDocument>("orders"), OrdersIndex, "orders-search-index.json", cancellationToken);
    }

    private static async Task CreateIndex(IMongoCollection<BsonDocument> collection, string indexName, string resourceName, CancellationToken cancellationToken)
    {
        var resource = Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .Single(name => name.EndsWith(resourceName, StringComparison.Ordinal));
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        var definition = BsonDocument.Parse(await reader.ReadToEndAsync(cancellationToken));
        await collection.SearchIndexes.CreateOneAsync(new CreateSearchIndexModel(indexName, definition), cancellationToken: cancellationToken);
    }
}
