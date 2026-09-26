using Microsoft.EntityFrameworkCore;
using SearchDB.Infrastructure.Postgres;
using SearchDB.Infrastructure.Postgres.Seed;
using Testcontainers.PostgreSql;
using Xunit;

namespace SearchDB.Infrastructure.Postgres.Tests;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("searchdb_tests")
        .WithUsername("searchdb")
        .WithPassword("searchdb-test-password")
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public async Task<SearchDbContext> CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<SearchDbContext>().UseNpgsql(_container.GetConnectionString()).Options;
        var db = new SearchDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE order_items, orders, products CASCADE");
        await PostgresSeed.EnsureSeededAsync(db);
        return db;
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
}
