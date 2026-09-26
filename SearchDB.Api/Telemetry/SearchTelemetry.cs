using System.Diagnostics;
using System.Diagnostics.Metrics;
using SearchDB.Application.Search;

namespace SearchDB.Api;

public sealed class SearchTelemetry
{
    public const string ActivitySourceName = "SearchDB.Search";
    public const string MeterName = "SearchDB.Search";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("search.duration", "ms");
    private static readonly Counter<long> Results = Meter.CreateCounter<long>("search.results");
    private static readonly Counter<long> Errors = Meter.CreateCounter<long>("search.errors");

    public async Task<SearchResult> ExecuteAsync(
        string provider,
        SearchRequest request,
        Func<SearchRequest, CancellationToken, Task<SearchResult>> execute,
        CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("search.execute", ActivityKind.Internal);
        activity?.SetTag("search.provider", provider);
        activity?.SetTag("search.entity", request.Entity.ToString().ToLowerInvariant());
        using var databaseActivity = ActivitySource.StartActivity("db.search", ActivityKind.Client);
        databaseActivity?.SetTag("db.system", provider == "postgres" ? "postgresql" : "mongodb");
        databaseActivity?.SetTag("db.operation", "search");
        databaseActivity?.SetTag("search.provider", provider);
        databaseActivity?.SetTag("search.entity", request.Entity.ToString().ToLowerInvariant());
        var started = Stopwatch.GetTimestamp();
        var tags = new TagList
        {
            { "search.provider", provider },
            { "search.entity", request.Entity.ToString().ToLowerInvariant() }
        };

        try
        {
            var result = await execute(request, cancellationToken);
            var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            activity?.SetTag("search.result_count", result.Items.Count);
            databaseActivity?.SetTag("search.result_count", result.Items.Count);
            Duration.Record(elapsed, tags);
            Results.Add(result.Items.Count, tags);
            return result with { DurationMilliseconds = elapsed };
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            databaseActivity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            Errors.Add(1, tags);
            throw;
        }
    }
}
