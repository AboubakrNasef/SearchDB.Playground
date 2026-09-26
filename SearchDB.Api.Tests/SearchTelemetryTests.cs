using System.Diagnostics;
using System.Diagnostics.Metrics;
using SearchDB.Api;
using SearchDB.Application.Search;
using Xunit;

namespace SearchDB.Api.Tests;

public class SearchTelemetryTests
{
    [Fact]
    public async Task Records_provider_and_scope_without_recording_search_text()
    {
        var activityTags = new List<KeyValuePair<string, string?>>();
        var activityNames = new List<string>();
        var metricTags = new List<KeyValuePair<string, object?>>();
        var resultMeasurements = new List<long>();
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == SearchTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                activityNames.Add(activity.OperationName);
                activityTags.AddRange(activity.Tags);
            }
        };
        ActivitySource.AddActivityListener(activityListener);

        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == SearchTelemetry.MeterName)
                    listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((_, _, tags, _) => metricTags.AddRange(tags.ToArray()));
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            metricTags.AddRange(tags.ToArray());
            if (instrument.Name == "search.results") resultMeasurements.Add(value);
        });
        meterListener.Start();

        var request = new SearchRequest(SearchEntity.Products, "private customer text", 1, 20, new());
        var result = await new SearchTelemetry().ExecuteAsync("postgres", request,
            (searchRequest, _) => Task.FromResult(new SearchResult(searchRequest.Entity,
                [new("1", "name", "sku", null, null, null, null, 0)], 1, 1, 20, 0)), CancellationToken.None);

        Assert.True(result.DurationMilliseconds >= 0);
        Assert.Contains(activityTags, tag => tag.Key == "search.provider" && Equals(tag.Value, "postgres"));
        Assert.Contains(activityTags, tag => tag.Key == "search.entity" && Equals(tag.Value, "products"));
        Assert.Contains("db.search", activityNames);
        Assert.Contains(metricTags, tag => tag.Key == "search.provider" && Equals(tag.Value, "postgres"));
        Assert.Contains(metricTags, tag => tag.Key == "search.entity" && Equals(tag.Value, "products"));
        Assert.Contains(1L, resultMeasurements);
        Assert.DoesNotContain(activityTags, tag => Equals(tag.Value, request.Query));
        Assert.DoesNotContain(metricTags, tag => Equals(tag.Value, request.Query));
    }
}
