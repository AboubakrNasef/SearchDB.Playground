using System.Diagnostics;
using Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Misc;
using SearchDB.Application.Search;
using SearchDB.Infrastructure.Mongo.Documents;

namespace SearchDB.Infrastructure.Mongo.Search;

public sealed class MongoSearch(IMongoDatabase database) : IMongoSearch
{
    public async Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            var result = request.Entity switch
            {
                SearchEntity.Products => await SearchProducts(request, cancellationToken),
                SearchEntity.Orders => await SearchOrders(request, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(request.Entity))
            };
            return result with { DurationMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds };
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException)
        {
            throw new SearchProviderUnavailableException("MongoDB search is unavailable or its search index is not ready.", exception);
        }
    }

    private async Task<SearchResult> SearchProducts(SearchRequest request, CancellationToken cancellationToken)
    {
        var stages = new List<BsonDocument>();
        if (!string.IsNullOrWhiteSpace(request.Query))
            stages.Add(new("$search", new BsonDocument
            {
                { "index", MongoSearchIndexes.ProductsIndex },
                { "text", new BsonDocument { { "query", request.Query.Trim() }, { "path", new BsonArray { "sku", "name", "category", "description" } } } }
            }));

        var filter = new BsonDocument();
        if (request.Filters.Category is not null) filter.Add("category", request.Filters.Category);
        if (request.Filters.Active.HasValue) filter.Add("isActive", request.Filters.Active.Value);
        AddFilterStage(stages, filter);
        return await RunFacetedSearch(database.GetCollection<ProductDocument>(MongoSearchCollections.Products), stages, request,
            new BsonDocument { { "_id", 1 }, { "name", 1 }, { "sku", 1 }, { "category", 1 }, { "unitPrice", 1 }, { "currency", 1 }, { "isActive", 1 }, { "score", 1 } },
            MapProduct, cancellationToken);
    }

    private async Task<SearchResult> SearchOrders(SearchRequest request, CancellationToken cancellationToken)
    {
        var stages = new List<BsonDocument>();
        if (!string.IsNullOrWhiteSpace(request.Query))
            stages.Add(new("$search", new BsonDocument
            {
                { "index", MongoSearchIndexes.OrdersIndex },
                { "text", new BsonDocument { { "query", request.Query.Trim() }, { "path", new BsonArray { "orderNumber", "status", "searchText" } } } }
            }));

        var filter = new BsonDocument();
        if (request.Filters.Status.HasValue) filter.Add("status", request.Filters.Status.Value.ToString());
        var created = new BsonDocument();
        if (request.Filters.CreatedFrom.HasValue) created.Add("$gte", new BsonDateTime(request.Filters.CreatedFrom.Value.UtcDateTime));
        if (request.Filters.CreatedTo.HasValue) created.Add("$lte", new BsonDateTime(request.Filters.CreatedTo.Value.UtcDateTime));
        if (created.ElementCount > 0) filter.Add("createdAt", created);
        AddFilterStage(stages, filter);
        return await RunFacetedSearch(database.GetCollection<OrderDocument>(MongoSearchCollections.Orders), stages, request,
            new BsonDocument { { "_id", 1 }, { "orderNumber", 1 }, { "status", 1 }, { "createdAt", 1 }, { "totalAmount", 1 }, { "score", 1 } },
            MapOrder, cancellationToken);
    }

    private static void AddFilterStage(List<BsonDocument> stages, BsonDocument filter)
    {
        if (filter.ElementCount > 0)
            stages.Add(new BsonDocument("$match", filter));
    }

    private static async Task<SearchResult> RunFacetedSearch<TDocument>(
        IMongoCollection<TDocument> collection,
        List<BsonDocument> stages,
        SearchRequest request,
        BsonDocument projection,
        Func<BsonDocument, SearchResultItem> map,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            stages.Add(new BsonDocument("$set", new BsonDocument("score", 0d)));
        else
            stages.Add(new BsonDocument("$set", new BsonDocument("score", new BsonDocument("$meta", "searchScore"))));

        var itemSort = string.IsNullOrWhiteSpace(request.Query)
            ? new BsonDocument("_id", 1)
            : new BsonDocument { { "score", -1 }, { "_id", 1 } };
        var pipeline = PipelineDefinition<TDocument, BsonDocument>.Create(stages.Append(new BsonDocument("$facet", new BsonDocument
        {
            { "items", new BsonArray
                {
                    new BsonDocument("$sort", itemSort),
                    new BsonDocument("$skip", (long)(request.Page - 1) * request.PageSize),
                    new BsonDocument("$limit", request.PageSize),
                    new BsonDocument("$project", projection)
                }
            },
            { "total", new BsonArray { new BsonDocument("$count", "count") } }
        })).ToArray());

        var facet = await collection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync(cancellationToken);
        var items = facet?["items"].AsBsonArray.Select(value => map(value.AsBsonDocument)).ToArray() ?? [];
        var counts = facet?["total"].AsBsonArray;
        long? total = counts is { Count: > 0 } ? counts[0]["count"].ToInt64() : 0L;
        return new(request.Entity, items, total, request.Page, request.PageSize, 0);
    }

    private static SearchResultItem MapProduct(BsonDocument row) => new(
        ReadGuid(row["_id"]).ToString(), row["name"].AsString,
        $"{row["sku"].AsString} · {row["currency"].AsString} {row["unitPrice"].ToDecimal():0.00}",
        row["category"].AsString, row["isActive"].AsBoolean ? "Active" : "Inactive",
        row["unitPrice"].ToDecimal(), null, row["score"].ToDouble());

    private static SearchResultItem MapOrder(BsonDocument row)
    {
        var status = row["status"].AsString;
        return new(ReadGuid(row["_id"]).ToString(), row["orderNumber"].AsString, status, null, status,
            row["totalAmount"].ToDecimal(), row["createdAt"].ToUniversalTime(), row["score"].ToDouble());
    }

    private static Guid ReadGuid(BsonValue value) => value.BsonType == BsonType.Binary
        ? value.AsBsonBinaryData.ToGuid(GuidRepresentation.Standard)
        : value.AsGuid;
}
