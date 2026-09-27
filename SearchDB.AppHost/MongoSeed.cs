using MongoDB.Driver;
using SearchDB.Infrastructure.Mongo.Documents;
using SearchDB.SeedData;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchDB.AppHost
{
    public static class MongoSeed
    {
        public static async Task SeededAsync(IMongoDatabase database, CancellationToken cancellationToken = default, int count = 100)
        {

            var fakes = FakerRecords.Generate(count);
            var generated = (fakes.Products, fakes.Orders);

            var products = database.GetCollection<ProductDocument>(MongoSearchCollections.Products);

            var productWrites = generated.Products.Select(product =>
                (WriteModel<ProductDocument>)new ReplaceOneModel<ProductDocument>(
                    Builders<ProductDocument>.Filter.Eq(p => p.Id, product.Id), ProductDocument.FromDomain(product))
                { IsUpsert = true });
            await products.BulkWriteAsync(productWrites, cancellationToken: cancellationToken);

            var orders = database.GetCollection<OrderDocument>(MongoSearchCollections.Orders);
            var orderWrites = generated.Orders.Select(order =>
                (WriteModel<OrderDocument>)new ReplaceOneModel<OrderDocument>(
                    Builders<OrderDocument>.Filter.Eq(o => o.Id, order.Id), OrderDocument.FromDomain(order))
                { IsUpsert = true });
            await orders.BulkWriteAsync(orderWrites, cancellationToken: cancellationToken);
        }
    }
}
