using MongoDB.Driver;
using SearchDB.Infrastructure.Mongo.Documents;
using SearchDB.SeedData;

namespace SearchDB.Infrastructure.Mongo.Seed;

public static class MongoSeed
{
    public static async Task EnsureSeededAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var products = database.GetCollection<ProductDocument>(MongoSearchCollections.Products);
        var productWrites = DemoRecords.Products.Select(product =>
            (WriteModel<ProductDocument>)new ReplaceOneModel<ProductDocument>(
                Builders<ProductDocument>.Filter.Eq(p => p.Id, product.Id), ProductDocument.FromDomain(product)) { IsUpsert = true });
        await products.BulkWriteAsync(productWrites, cancellationToken: cancellationToken);

        var orders = database.GetCollection<OrderDocument>(MongoSearchCollections.Orders);
        var orderWrites = DemoRecords.Orders.Select(order =>
            (WriteModel<OrderDocument>)new ReplaceOneModel<OrderDocument>(
                Builders<OrderDocument>.Filter.Eq(o => o.Id, order.Id), OrderDocument.FromDomain(order)) { IsUpsert = true });
        await orders.BulkWriteAsync(orderWrites, cancellationToken: cancellationToken);
    }
}
