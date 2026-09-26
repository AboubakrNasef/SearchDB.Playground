using System.Net;
using System.Net.Http.Json;
using Domain;
using SearchDB.Application.Search;
using Xunit;

namespace SearchDB.Api.Tests;

public class SearchEndpointTests
{
    [Fact]
    public async Task Postgres_endpoint_calls_postgres_provider_and_returns_shared_result()
    {
        using var factory = new SearchApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/search/postgres?entity=Products&query=keyboard&category=Accessories&active=true");
        var result = await response.Content.ReadFromJsonAsync<SearchResult>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SearchEntity.Products, factory.Postgres.LastRequest?.Entity);
        Assert.Null(factory.Mongo.LastRequest);
        Assert.Equal(SearchEntity.Products, result?.Entity);
        Assert.Single(result!.Items);
    }

    [Fact]
    public async Task Mongo_endpoint_calls_mongo_provider()
    {
        using var factory = new SearchApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/search/mongo?entity=Orders&status=Pending");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(factory.Postgres.LastRequest);
        Assert.Equal(SearchEntity.Orders, factory.Mongo.LastRequest?.Entity);
        Assert.Equal(OrderStatus.Pending, factory.Mongo.LastRequest?.Filters.Status);
    }

    [Theory]
    [InlineData("?entity=Products&page=0")]
    [InlineData("?entity=Products&pageSize=101")]
    [InlineData("?entity=Products&status=Pending")]
    [InlineData("?entity=Orders&createdFrom=2026-02-01T00:00:00Z&createdTo=2026-01-01T00:00:00Z")]
    public async Task Invalid_search_parameters_return_400(string query)
    {
        using var factory = new SearchApiFactory();

        var response = await factory.CreateClient().GetAsync($"/api/search/postgres{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Provider_unavailable_returns_safe_503_problem_response()
    {
        using var factory = new SearchApiFactory();
        factory.Postgres.FailWithUnavailable = true;

        var response = await factory.CreateClient().GetAsync("/api/search/postgres?entity=Products&query=keyboard");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("search provider", body, StringComparison.OrdinalIgnoreCase);
    }
}
