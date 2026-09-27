using Domain;
using MongoDB.Driver;
using SearchDB.Infrastructure.Mongo.Documents;

namespace SearchDB.AppHost;

public static class MongoSeed
{
    public static async Task SeedProductsAsync(
        IMongoDatabase database,
        IReadOnlyList<Product> productRecords,
        CancellationToken cancellationToken = default)
    {
        var products = database.GetCollection<ProductDocument>(MongoSearchCollections.Products);
        var productWrites = productRecords.Select(product =>
            (WriteModel<ProductDocument>)new ReplaceOneModel<ProductDocument>(
                Builders<ProductDocument>.Filter.Eq(p => p.Id, product.Id), ProductDocument.FromDomain(product))
            { IsUpsert = true });
        await products.BulkWriteAsync(productWrites, cancellationToken: cancellationToken);
    }

    public static async Task SeedOrdersAsync(IMongoDatabase database, IReadOnlyList<Order> orderRecords, CancellationToken cancellationToken = default)
    {
        var orders = database.GetCollection<OrderDocument>(MongoSearchCollections.Orders);
        await orders.InsertManyAsync(orderRecords.Select(OrderDocument.FromDomain), cancellationToken: cancellationToken);
    }
}
