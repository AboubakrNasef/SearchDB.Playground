using Domain;
using Xunit;

namespace Domain.Tests;

public class ProductAndOrderTests
{
    [Fact]
    public void Product_requires_sku_name_category_currency_and_nonnegative_price()
    {
        Assert.Throws<ArgumentException>(() => new Product(Guid.NewGuid(), " ", "Widget", "Tools", "Useful", 5m, "USD", true));
        Assert.Throws<ArgumentException>(() => new Product(Guid.NewGuid(), "W-1", " ", "Tools", "Useful", 5m, "USD", true));
        Assert.Throws<ArgumentException>(() => new Product(Guid.NewGuid(), "W-1", "Widget", "Tools", "Useful", -1m, "USD", true));
        Assert.Throws<ArgumentException>(() => new Product(Guid.NewGuid(), "W-1", "Widget", "Tools", "Useful", 5m, "usd", true));
    }

    [Fact]
    public void Order_item_requires_positive_quantity_and_calculates_decimal_line_total()
    {
        var item = new OrderItem(Guid.NewGuid(), Guid.NewGuid(), "W-1", "Widget", 1.25m, 3);

        Assert.Equal(3.75m, item.LineTotal);
        Assert.Throws<ArgumentOutOfRangeException>(() => new OrderItem(Guid.NewGuid(), Guid.NewGuid(), "W-1", "Widget", 1m, 0));
    }

    [Fact]
    public void Order_requires_at_least_one_item_and_preserves_item_snapshot()
    {
        Assert.Throws<ArgumentException>(() => new Order(Guid.NewGuid(), "O-1", DateTimeOffset.UtcNow, OrderStatus.Pending, []));

        var productId = Guid.NewGuid();
        var item = new OrderItem(Guid.NewGuid(), productId, "OLD-SKU", "Old name", 4m, 2);
        var order = new Order(Guid.NewGuid(), "O-1", DateTimeOffset.UtcNow, OrderStatus.Pending, [item]);

        Assert.Equal("OLD-SKU", order.Items[0].ProductSkuSnapshot);
        Assert.Equal("Old name", order.Items[0].ProductNameSnapshot);
        Assert.Equal(8m, order.Items[0].LineTotal);
    }

    [Fact]
    public void Order_search_text_contains_order_status_and_item_snapshot_terms()
    {
        var order = new Order(Guid.NewGuid(), "ORD-123", DateTimeOffset.UtcNow, OrderStatus.Pending,
            [new(Guid.NewGuid(), Guid.NewGuid(), "SKU-OLD", "Old keyboard", 5m, 1)]);

        Assert.Contains("ORD-123", order.SearchText);
        Assert.Contains("Pending", order.SearchText);
        Assert.Contains("SKU-OLD", order.SearchText);
        Assert.Contains("Old keyboard", order.SearchText);
    }
}
