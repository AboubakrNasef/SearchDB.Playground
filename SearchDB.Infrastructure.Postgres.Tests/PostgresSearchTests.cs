using Domain;
using Microsoft.EntityFrameworkCore;
using SearchDB.Application.Search;
using SearchDB.Infrastructure.Postgres;
using SearchDB.Infrastructure.Postgres.Search;
using Xunit;

namespace SearchDB.Infrastructure.Postgres.Tests;

[Collection("postgres")]
public class PostgresSearchTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task Product_search_matches_seeded_text_and_category_filter()
    {
        await using var db = await fixture.CreateDatabase();
        var marker = $"pg-{Guid.NewGuid():N}";
        var product = new Product(Guid.NewGuid(), $"SKU-{marker}", $"Widget {marker}", "TestCategory", "special searchable item", 12.50m, "USD", true);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var result = await new PostgresSearch(db).SearchAsync(
            new(SearchEntity.Products, "searchable", 1, 20, new(Category: "TestCategory", Active: true)), CancellationToken.None);

        Assert.Contains(result.Items, item => item.Id == product.Id.ToString());
    }

    [Fact]
    public async Task Shared_demo_seed_supports_the_same_product_browse_as_mongo()
    {
        await using var db = await fixture.CreateDatabase();

        var result = await new PostgresSearch(db).SearchAsync(
            new(SearchEntity.Products, "", 1, 2, new()), CancellationToken.None);

        Assert.Equal(4, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("11111111-1111-1111-1111-111111111111", result.Items[0].Id);
        Assert.Equal("Mechanical Keyboard", result.Items[0].PrimaryText);
    }

    [Fact]
    public async Task Order_search_matches_order_number_and_status_filter()
    {
        await using var db = await fixture.CreateDatabase();
        var marker = $"ORD-{Guid.NewGuid():N}";
        var productId = Guid.NewGuid();
        db.Products.Add(new Product(productId, $"SKU-{marker}", "Snapshot Product", "TestCategory", "Test description", 4m, "USD", true));
        var order = new Order(Guid.NewGuid(), marker, DateTimeOffset.UtcNow, OrderStatus.Pending,
            [new(Guid.NewGuid(), productId, "SNAP-1", "Snapshot Product", 4m, 2)]);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var result = await new PostgresSearch(db).SearchAsync(
            new(SearchEntity.Orders, marker, 1, 20, new(Status: OrderStatus.Pending)), CancellationToken.None);

        Assert.Contains(result.Items, item => item.Id == order.Id.ToString());
        Assert.Equal(8m, result.Items.Single(item => item.Id == order.Id.ToString()).Amount);
    }
}
