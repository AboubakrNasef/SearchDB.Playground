using System.Diagnostics;
using Domain;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
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

    private Task<SearchResult> SearchProducts(SearchRequest request, CancellationToken cancellationToken)
    {
        var filters = Builders<ProductDocument>.Filter;
        var filter = filters.And(
            request.Filters.Category is null ? filters.Empty : filters.Eq(product => product.Category, request.Filters.Category),
            request.Filters.Active is null ? filters.Empty : filters.Eq(product => product.IsActive, request.Filters.Active.Value));

        var search = string.IsNullOrWhiteSpace(request.Query)
            ? null
            : Builders<ProductDocument>.Search.Text(
                Builders<ProductDocument>.SearchPath.Multi(product => product.Sku, product => product.Name, product => product.Category, product => product.Description),
                request.Query.Trim());
        var projection = Builders<ProductDocument>.Projection
            .Include(product => product.Id)
            .Include(product => product.Name)
            .Include(product => product.Sku)
            .Include(product => product.Category)
            .Include(product => product.UnitPrice)
            .Include(product => product.Currency)
            .Include(product => product.IsActive);
        if (search is not null)
            projection = projection.MetaSearchScore("score");

        var sort = search is null
            ? Builders<ProductDocument>.Sort.Ascending(product => product.Id)
            : Builders<ProductDocument>.Sort.MetaSearchScoreDescending().Ascending(product => product.Id);

        return RunSearch(database.GetCollection<ProductDocument>(MongoSearchCollections.Products), search,
            MongoSearchIndexes.ProductsIndex, filter, sort, projection, request,
            hit => new SearchResultItem(hit.Id.ToString(), hit.Name!,
                $"{hit.Sku} · {hit.Currency} {hit.UnitPrice:0.00}", hit.Category,
                hit.IsActive ? "Active" : "Inactive", hit.UnitPrice, null, hit.Score), cancellationToken);
    }

    private Task<SearchResult> SearchOrders(SearchRequest request, CancellationToken cancellationToken)
    {
        var filters = Builders<OrderDocument>.Filter;
        var filter = filters.And(
            request.Filters.Status is null ? filters.Empty : filters.Eq(order => order.Status, request.Filters.Status.Value.ToString()),
            request.Filters.CreatedFrom is null ? filters.Empty : filters.Gte(order => order.CreatedAt, request.Filters.CreatedFrom.Value.UtcDateTime),
            request.Filters.CreatedTo is null ? filters.Empty : filters.Lte(order => order.CreatedAt, request.Filters.CreatedTo.Value.UtcDateTime));

        var search = string.IsNullOrWhiteSpace(request.Query)
            ? null
            : Builders<OrderDocument>.Search.Text(
                Builders<OrderDocument>.SearchPath.Multi(order => order.OrderNumber, order => order.Status, order => order.SearchText),
                request.Query.Trim());
        var projection = Builders<OrderDocument>.Projection
            .Include(order => order.Id)
            .Include(order => order.OrderNumber)
            .Include(order => order.Status)
            .Include(order => order.CreatedAt)
            .Include(order => order.TotalAmount);
        if (search is not null)
            projection = projection.MetaSearchScore("score");

        var sort = search is null
            ? Builders<OrderDocument>.Sort.Descending(order => order.CreatedAt).Ascending(order => order.Id)
            : Builders<OrderDocument>.Sort.MetaSearchScoreDescending().Descending(order => order.CreatedAt).Ascending(order => order.Id);

        return RunSearch(database.GetCollection<OrderDocument>(MongoSearchCollections.Orders), search,
            MongoSearchIndexes.OrdersIndex, filter, sort, projection, request,
            hit => new SearchResultItem(hit.Id.ToString(), hit.OrderNumber!, hit.Status!, null,
                hit.Status, hit.TotalAmount, hit.CreatedAt, hit.Score), cancellationToken);
    }

    private static async Task<SearchResult> RunSearch<TDocument>(
        IMongoCollection<TDocument> collection,
        MongoDB.Driver.Search.SearchDefinition<TDocument>? search,
        string indexName,
        FilterDefinition<TDocument> filter,
        SortDefinition<TDocument> sort,
        ProjectionDefinition<TDocument> projection,
        SearchRequest request,
        Func<SearchHit, SearchResultItem> map,
        CancellationToken cancellationToken)
    {
        IAggregateFluent<TDocument> aggregate = collection.Aggregate();
        if (search is not null)
            aggregate = aggregate.Search(search, new MongoDB.Driver.Search.SearchOptions<TDocument> { IndexName = indexName });
        aggregate = aggregate.Match(filter);

        var count = await aggregate.Count().FirstOrDefaultAsync(cancellationToken);
        var hits = await aggregate.Sort(sort)
            .Skip((request.Page - 1) * request.PageSize)
            .Limit(request.PageSize)
            .Project<SearchHit>(projection)
            .ToListAsync(cancellationToken);

        return new(request.Entity, hits.Select(map).ToArray(), count?.Count ?? 0,
            request.Page, request.PageSize, 0);
    }

    private sealed class SearchHit
    {
        [BsonId]
        [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
        public Guid Id { get; set; }
        [BsonElement("name")] public string? Name { get; set; }
        [BsonElement("sku")] public string? Sku { get; set; }
        [BsonElement("category")] public string? Category { get; set; }
        [BsonElement("currency")] public string? Currency { get; set; }
        [BsonElement("isActive")] public bool IsActive { get; set; }
        [BsonElement("orderNumber")] public string? OrderNumber { get; set; }
        [BsonElement("status")] public string? Status { get; set; }
        [BsonElement("createdAt")] public DateTimeOffset? CreatedAt { get; set; }
        [BsonElement("totalAmount")] public decimal TotalAmount { get; set; }
        [BsonElement("unitPrice")] public decimal UnitPrice { get; set; }
        [BsonElement("score")] public double Score { get; set; }
    }
}
