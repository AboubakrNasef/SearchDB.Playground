using Domain;
using Microsoft.EntityFrameworkCore;
using SearchDB.Infrastructure.Postgres;

namespace SearchDB.AppHost;

public static class PostgresSeed
{
    public static async Task SeedProductsAsync(
        SearchDbContext db,
        IReadOnlyList<Product> products,
        CancellationToken cancellationToken = default)
    {
        var productIds = products.Select(p => p.Id).ToHashSet();
        var existingProducts = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        foreach (var product in products)
        {
            if (existingProducts.TryGetValue(product.Id, out var existing))
                db.Entry(existing).CurrentValues.SetValues(product);
            else
                db.Products.Add(product);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task SeedOrdersAsync(SearchDbContext db, IReadOnlyList<Order> orders, CancellationToken cancellationToken = default)
    {
        db.Orders.AddRange(orders);
        await db.SaveChangesAsync(cancellationToken);
    }
}
