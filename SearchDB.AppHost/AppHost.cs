using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using SearchDB.AppHost;
using SearchDB.Infrastructure.Postgres;
using SearchDB.SeedData;

var builder = DistributedApplication.CreateBuilder(args);

var name = builder.AddParameter("database-user", "user", secret: false);
var password = builder.AddParameter("database-password", "password");
var postgres = builder.AddPostgres("postgres", name, password, 5454)
    .WithVolume("pdVol", "/var/postgresql/").WithPgAdmin();
var database = postgres.AddDatabase("pg-searchdb");
var mongoDb = builder.AddMongoDB("mongo", 27029, name, password).WithDataVolume();
var mongoDatabaseName = "mongo-searchdb";
var mongoDatabase = mongoDb.AddDatabase(mongoDatabaseName);

var api = builder.AddProject<Projects.SearchDB_Api>("api")
    .WithReference(database)
    .WithReference(mongoDatabase)
    .WaitFor(database)
    .WaitFor(mongoDatabase)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpHealthCheck("/health")
    .WithCommand("SeedProducts", "Seed fixed product catalog", async context =>
    {
        try
        {
            var postgresConnectionString = await database.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);
            var mongoConnectionString = await mongoDb.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);
            var options = new DbContextOptionsBuilder<SearchDbContext>().UseNpgsql(postgresConnectionString).Options;
            await using var db = new SearchDbContext(options);
            await PostgresSeed.SeedProductsAsync(db, DemoRecords.Products, context.CancellationToken);
            var mongo = new MongoClient(mongoConnectionString).GetDatabase(mongoDatabaseName);
            await MongoSeed.SeedProductsAsync(mongo, DemoRecords.Products, context.CancellationToken);
            return CommandResults.Success("Fixed product catalog seeded to PostgreSQL and MongoDB.");
        }
        catch (Exception exception)
        {
            return CommandResults.Failure(exception);
        }
    })
    .WithCommand("SeedOrders", "Add 1,000 generated orders", async context =>
    {
        try
        {
            var postgresConnectionString = await database.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);
            var mongoConnectionString = await mongoDb.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);
            var orders = FakerRecords.GenerateOrders(1000);
            var options = new DbContextOptionsBuilder<SearchDbContext>().UseNpgsql(postgresConnectionString).Options;
            await using var db = new SearchDbContext(options);
            await PostgresSeed.SeedProductsAsync(db, DemoRecords.Products, context.CancellationToken);
            await PostgresSeed.SeedOrdersAsync(db, orders, context.CancellationToken);
            var mongo = new MongoClient(mongoConnectionString).GetDatabase(mongoDatabaseName);
            await MongoSeed.SeedProductsAsync(mongo, DemoRecords.Products, context.CancellationToken);
            await MongoSeed.SeedOrdersAsync(mongo, orders, context.CancellationToken);
            return CommandResults.Success("1,000 new orders seeded to PostgreSQL and MongoDB.");
        }
        catch (Exception exception)
        {
            return CommandResults.Failure(exception);
        }
    })
    .WithCommand("ClearSampleData", "Clear databases", async context =>
    {
        var failures = new List<Exception>();

        try
        {
            var postgresConnectionString = await database.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);
            var options = new DbContextOptionsBuilder<SearchDbContext>().UseNpgsql(postgresConnectionString).Options;
            await using (var db = new SearchDbContext(options))
            {
                await db.OrderItems.ExecuteDeleteAsync(context.CancellationToken);
                await db.Orders.ExecuteDeleteAsync(context.CancellationToken);
                await db.Products.ExecuteDeleteAsync(context.CancellationToken);
            }
        }
        catch (Exception exception)
        {
            failures.Add(new InvalidOperationException("PostgreSQL clear failed.", exception));
        }

        try
        {
            var mongoConnectionString = await mongoDb.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);
            await new MongoClient(mongoConnectionString).DropDatabaseAsync(mongoDatabaseName, context.CancellationToken);
        }
        catch (Exception exception)
        {
            failures.Add(new InvalidOperationException("MongoDB clear failed.", exception));
        }

        return failures.Count == 0
            ? CommandResults.Success("Sample data cleared from PostgreSQL and MongoDB.")
            : CommandResults.Failure(new AggregateException("One or more database clears failed.", failures));
    }, new() { ConfirmationMessage = "Clear sample data from both PostgreSQL and MongoDB?" });

builder.AddViteApp("frontend", "../frontend")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("VITE_API_ORIGIN", api.GetEndpoint("http"));

builder.Build().Run();
