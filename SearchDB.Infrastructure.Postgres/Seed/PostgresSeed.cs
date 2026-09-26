using Microsoft.EntityFrameworkCore;
using SearchDB.SeedData;

namespace SearchDB.Infrastructure.Postgres.Seed;

public static class PostgresSeed
{
    public static async Task EnsureSeededAsync(SearchDbContext db, CancellationToken cancellationToken = default)
    {
        var productIds = DemoRecords.Products.Select(p => p.Id).ToHashSet();
        var existingProducts = await db.Products.AsNoTracking().Where(p => productIds.Contains(p.Id)).Select(p => p.Id).ToListAsync(cancellationToken);
        db.Products.AddRange(DemoRecords.Products.Where(p => !existingProducts.Contains(p.Id)));
        await db.SaveChangesAsync(cancellationToken);

        var orderIds = DemoRecords.Orders.Select(o => o.Id).ToHashSet();
        var existingOrders = await db.Orders.AsNoTracking().Where(o => orderIds.Contains(o.Id)).Select(o => o.Id).ToListAsync(cancellationToken);
        db.Orders.AddRange(DemoRecords.Orders.Where(o => !existingOrders.Contains(o.Id)));
        await db.SaveChangesAsync(cancellationToken);
    }
}
