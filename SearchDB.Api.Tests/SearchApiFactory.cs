using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SearchDB.Application.Search;

namespace SearchDB.Api.Tests;

public sealed class SearchApiFactory : WebApplicationFactory<Program>
{
    public RecordingSearch Postgres { get; } = new();
    public RecordingSearch Mongo { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:searchdb"] = "Host=localhost;Database=tests;Username=test;Password=test",
                ["Database:InitializeOnStartup"] = "false"
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPostgresSearch>();
            services.RemoveAll<IMongoSearch>();
            services.AddSingleton<IPostgresSearch>(Postgres);
            services.AddSingleton<IMongoSearch>(Mongo);
        });
    }
}

public sealed class RecordingSearch : IPostgresSearch, IMongoSearch
{
    public SearchRequest? LastRequest { get; private set; }
    public bool FailWithUnavailable { get; set; }

    public Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        if (FailWithUnavailable)
            throw new SearchProviderUnavailableException("Search provider is temporarily unavailable.");

        return Task.FromResult(new SearchResult(request.Entity,
            [new("test-id", "result", "details", "Accessories", null, null, null, 1)],
            1, request.Page, request.PageSize, 1.25));
    }
}
