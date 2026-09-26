using Microsoft.AspNetCore.Mvc;
using SearchDB.Application.Search;

namespace SearchDB.Api.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/search");
        group.MapGet("/postgres", ( [AsParameters] SearchQuery query, IPostgresSearch search, SearchTelemetry telemetry, CancellationToken cancellationToken) =>
            ExecuteAsync("postgres", query, search.SearchAsync, telemetry, cancellationToken));
        group.MapGet("/mongo", ([AsParameters] SearchQuery query, IMongoSearch search, SearchTelemetry telemetry, CancellationToken cancellationToken) =>
            ExecuteAsync("mongo", query, search.SearchAsync, telemetry, cancellationToken));
        return endpoints;
    }

    private static async Task<IResult> ExecuteAsync(
        string provider,
        SearchQuery query,
        Func<SearchRequest, CancellationToken, Task<SearchResult>> search,
        SearchTelemetry telemetry,
        CancellationToken cancellationToken)
    {
        var request = query.ToRequest();
        var errors = SearchRequestValidator.Validate(request).ToList();
        if (query.Entity is null)
            errors.Add(new("entity", "Entity is required and must be products or orders."));
        if (errors.Count > 0)
            return Results.ValidationProblem(errors.GroupBy(error => error.Field)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray()));

        return Results.Ok(await telemetry.ExecuteAsync(provider, request, search, cancellationToken));
    }

    public sealed class SearchQuery
    {
        [FromQuery(Name = "entity")]
        public SearchEntity? Entity { get; set; }
        [FromQuery(Name = "query")]
        public string? Query { get; set; }
        [FromQuery(Name = "page")]
        public int? Page { get; set; }
        [FromQuery(Name = "pageSize")]
        public int? PageSize { get; set; }
        [FromQuery(Name = "category")]
        public string? Category { get; set; }
        [FromQuery(Name = "active")]
        public bool? Active { get; set; }
        [FromQuery(Name = "status")]
        public Domain.OrderStatus? Status { get; set; }
        [FromQuery(Name = "createdFrom")]
        public DateTimeOffset? CreatedFrom { get; set; }
        [FromQuery(Name = "createdTo")]
        public DateTimeOffset? CreatedTo { get; set; }

        public SearchRequest ToRequest() => new(
            Entity ?? SearchEntity.Products,
            Query ?? string.Empty,
            Page ?? 1,
            PageSize ?? 20,
            new(Category, Active, Status, CreatedFrom, CreatedTo));
    }
}
