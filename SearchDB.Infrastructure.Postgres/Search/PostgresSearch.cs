using System.Diagnostics;
using Domain;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Npgsql;
using SearchDB.Application.Search;

namespace SearchDB.Infrastructure.Postgres.Search;

public sealed class PostgresSearch(SearchDbContext db) : IPostgresSearch
{
    public async Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            var query = request.Query.Trim();
            SearchResult result = request.Entity switch
            {
                SearchEntity.Products => await SearchProducts(request, query, cancellationToken),
                SearchEntity.Orders => await SearchOrders(request, query, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(request.Entity))
            };
            return result with { DurationMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds };
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException)
        {
            throw new SearchProviderUnavailableException("PostgreSQL search is unavailable.", exception);
        }
    }

    private async Task<SearchResult> SearchProducts(SearchRequest request, string query, CancellationToken cancellationToken)
    {
        var products = db.Products.AsNoTracking();
        if (request.Filters.Category is not null)
            products = products.Where(p => p.Category == request.Filters.Category);
        if (request.Filters.Active.HasValue)
            products = products.Where(p => p.IsActive == request.Filters.Active.Value);

        var ranked = query.Length > 0;
        if (ranked)
        {
            var pattern = LikePattern(query);
            products = products.Where(p =>
                EF.Property<NpgsqlTsVector>(p, "SearchVector").Matches(EF.Functions.PlainToTsQuery("english", query)) ||
                EF.Functions.ILike(p.Sku, pattern, "\\") ||
                EF.Functions.ILike(p.Name, pattern, "\\") ||
                EF.Functions.ILike(p.Category, pattern, "\\") ||
                EF.Functions.ILike(p.Description, pattern, "\\"));
        }

        var total = await products.LongCountAsync(cancellationToken);
        var rows = await products
            .OrderByDescending(p => ranked ? EF.Property<NpgsqlTsVector>(p, "SearchVector").Rank(EF.Functions.PlainToTsQuery("english", query)) : 0f)
            .ThenBy(p => p.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Sku,
                p.Category,
                p.UnitPrice,
                p.Currency,
                p.IsActive,
                Score = ranked ? EF.Property<NpgsqlTsVector>(p, "SearchVector").Rank(EF.Functions.PlainToTsQuery("english", query)) : 0f
            })
            .ToListAsync(cancellationToken);

        return new(SearchEntity.Products, rows.Select(p => new SearchResultItem(
            p.Id.ToString(), p.Name, $"{p.Sku} · {p.Currency} {p.UnitPrice:0.00}", p.Category,
            p.IsActive ? "Active" : "Inactive", p.UnitPrice, null, p.Score)).ToArray(), total,
            request.Page, request.PageSize, 0);
    }

    private async Task<SearchResult> SearchOrders(SearchRequest request, string query, CancellationToken cancellationToken)
    {
        var orders = db.Orders.AsNoTracking();
        if (request.Filters.Status.HasValue)
            orders = orders.Where(o => o.Status == request.Filters.Status.Value);
        if (request.Filters.CreatedFrom.HasValue)
            orders = orders.Where(o => o.CreatedAt >= request.Filters.CreatedFrom.Value);
        if (request.Filters.CreatedTo.HasValue)
            orders = orders.Where(o => o.CreatedAt <= request.Filters.CreatedTo.Value);

        var ranked = query.Length > 0;
        if (ranked)
        {
            var pattern = LikePattern(query);
            orders = orders.Where(o =>
                EF.Property<NpgsqlTsVector>(o, "SearchVector").Matches(EF.Functions.PlainToTsQuery("english", query)) ||
                EF.Functions.ILike(o.OrderNumber, pattern, "\\") ||
                EF.Functions.ILike(o.SearchText, pattern, "\\") ||
                o.Items.Any(i => EF.Property<NpgsqlTsVector>(i, "SearchVector").Matches(EF.Functions.PlainToTsQuery("english", query))));
        }

        var total = await orders.LongCountAsync(cancellationToken);
        var rows = await orders
            .OrderByDescending(o => ranked ? EF.Property<NpgsqlTsVector>(o, "SearchVector").Rank(EF.Functions.PlainToTsQuery("english", query)) : 0f)
            .ThenByDescending(o => o.CreatedAt)
            .ThenBy(o => o.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                o.CreatedAt,
                Amount = o.Items.Sum(i => i.UnitPriceSnapshot * i.Quantity),
                Score = ranked ? EF.Property<NpgsqlTsVector>(o, "SearchVector").Rank(EF.Functions.PlainToTsQuery("english", query)) : 0f
            })
            .ToListAsync(cancellationToken);

        return new(SearchEntity.Orders, rows.Select(o => new SearchResultItem(
            o.Id.ToString(), o.OrderNumber, o.Status.ToString(), null, o.Status.ToString(), o.Amount, o.CreatedAt, o.Score)).ToArray(), total,
            request.Page, request.PageSize, 0);
    }

    // ponytail: substring matching scans text columns; add pg_trgm indexes if search volume makes it slow.
    private static string LikePattern(string query) => $"%{query.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
}
