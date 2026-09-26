using SearchDB.Application.Search;
using SearchDB.Infrastructure.Mongo.Search;
using Xunit;

namespace SearchDB.Infrastructure.Mongo.Tests;

[Collection("atlas-local")]
public class MongoSearchTests(AtlasLocalContainerFixture fixture)
{
    [Fact]
    public async Task Search_index_initializer_can_run_again_for_application_restarts()
    {
        await MongoSearchIndexes.EnsureCreatedAsync(fixture.Database);
    }

    [Fact]
    public async Task Product_search_matches_seeded_text_and_category_filter()
    {
        var result = await new MongoSearch(fixture.Database).SearchAsync(
            new(SearchEntity.Products, "keyboard", 1, 20, new(Category: "Accessories", Active: true)), CancellationToken.None);

        var product = Assert.Single(result.Items);
        Assert.Equal("Mechanical Keyboard", product.PrimaryText);
        Assert.True(product.Score > 0);
    }

    [Fact]
    public async Task Order_search_matches_item_snapshot_and_status_filter()
    {
        var result = await new MongoSearch(fixture.Database).SearchAsync(
            new(SearchEntity.Orders, "keyboard", 1, 20, new(Status: Domain.OrderStatus.Completed)), CancellationToken.None);

        var order = Assert.Single(result.Items);
        Assert.Equal("ORD-1001", order.PrimaryText);
        Assert.Equal(129.49m, order.Amount);
    }

    [Fact]
    public async Task Browse_returns_page_with_stable_total_count()
    {
        var result = await new MongoSearch(fixture.Database).SearchAsync(
            new(SearchEntity.Products, "", 1, 2, new()), CancellationToken.None);

        Assert.Equal(4, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("11111111-1111-1111-1111-111111111111", result.Items[0].Id);
    }
}
