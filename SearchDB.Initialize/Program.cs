using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using SearchDB.Infrastructure.Mongo.Search;
using SearchDB.Infrastructure.Mongo.Seed;
using SearchDB.Infrastructure.Postgres;
using SearchDB.Infrastructure.Postgres.Seed;

var postgresConnection = Environment.GetEnvironmentVariable("ConnectionStrings__pg-searchdb")
    ?? throw new InvalidOperationException("Connection string 'pg-searchdb' is required.");

var options = new DbContextOptionsBuilder<SearchDbContext>().UseNpgsql(postgresConnection).Options;
await using (var postgres = new SearchDbContext(options))
{
    await postgres.Database.EnsureCreatedAsync();
    await PostgresSeed.EnsureSeededAsync(postgres);
}

var mongoConnection = Environment.GetEnvironmentVariable("ConnectionStrings__mongo-searchdb");
if (!string.IsNullOrWhiteSpace(mongoConnection))
{
    var mongoDatabase = new MongoClient(mongoConnection)
        .GetDatabase(Environment.GetEnvironmentVariable("Mongo__Database") ?? "searchdb");
    try
    {
        await MongoSeed.EnsureSeededAsync(mongoDatabase);
        await MongoSearchIndexes.EnsureCreatedAsync(mongoDatabase);
    }
    catch (Exception exception) when (exception is MongoException or TimeoutException)
    {
        Console.Error.WriteLine($"MongoDB initialization failed: {exception.Message}");
        return 1;
    }
}

Console.WriteLine("Database initialization complete.");
return 0;
