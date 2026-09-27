using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SearchDB.Api;
using SearchDB.Api.Endpoints;
using SearchDB.Api.Errors;
using SearchDB.Application.Search;
using SearchDB.Infrastructure.Postgres;
using SearchDB.Infrastructure.Postgres.Search;
using SearchDB.Infrastructure.Mongo.Search;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddSingleton<SearchTelemetry>();
builder.Services.AddExceptionHandler<SearchExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(SearchTelemetry.ActivitySourceName)
        .AddSource("Npgsql")
        .AddSource(MongoTelemetry.ActivitySourceName))
    .WithMetrics(metrics => metrics.AddMeter(SearchTelemetry.MeterName));

var postgresConnection = builder.Configuration.GetConnectionString("pg-searchdb");
builder.Services.AddDbContext<SearchDbContext>(options => options.UseNpgsql(postgresConnection));
builder.Services.AddScoped<IPostgresSearch, PostgresSearch>();

var mongoConnection = builder.Configuration.GetConnectionString("mongo-searchdb");
if (string.IsNullOrWhiteSpace(mongoConnection))
{
    builder.Services.AddSingleton<IMongoSearch, MissingMongoSearch>();
}
else
{
    var mongoDatabaseName = builder.Configuration["Mongo:Database"] ?? "searchdb";
    var mongoSettings = MongoClientSettings.FromConnectionString(mongoConnection);
    mongoSettings.TracingOptions = new() { QueryTextMaxLength = 4096 };
    builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoSettings));
    builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabaseName));
    builder.Services.AddSingleton<IMongoSearch, MongoSearch>();
}

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<SearchDbContext>().Database.EnsureCreatedAsync();

app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapSearchEndpoints();

app.Run();

public partial class Program
{
}

internal sealed class MissingMongoSearch : IMongoSearch
{
    public Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken) =>
        throw new SearchProviderUnavailableException("MongoDB connection is not configured.");
}
