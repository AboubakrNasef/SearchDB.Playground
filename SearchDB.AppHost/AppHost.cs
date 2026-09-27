using MongoDB.Driver;
using SearchDB.AppHost;


var builder = DistributedApplication.CreateBuilder(args);

var name = builder.AddParameter("database-user", "user", secret: false);
var password = builder.AddParameter("database-password", "password");
var postgres = builder.AddPostgres("postgres", name, password, 5454)
    .WithVolume("pdVol", "/var/postgresql/").WithPgAdmin();

var database = postgres.AddDatabase("pg-searchdb");
var mongoDb = builder.AddMongoDB("mongo", 27029, name, password)
    .WithDataVolume();
var mongoDatabaseName = "mongo-searchdb";
var cmdOpt = new CommandOptions();
cmdOpt.
var mongoDatabase = mongoDb.AddDatabase("mongo-searchdb")
    .WithCommand("SeedSampleData", "Seed sample data", async (context) =>
    {
        try
        {

            var connectionString = await mongoDb.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);
            var database = new MongoClient(connectionString).GetDatabase(mongoDatabaseName);
            await MongoSeed.SeededAsync(database, context.CancellationToken, 1000);
            return CommandResults.Success("MongoDB sample data seeded.");
        }
        catch (Exception exception)
        {
            return CommandResults.Failure(exception);
        }
    },)
  .WithCommand("ClearSampleData", "Clear database", ClearMongoDb(mongoDb, mongoDatabaseName), new() { ConfirmationMessage = "Drop the entire MongoDB database?" });


var api = builder.AddProject<Projects.SearchDB_Api>("api")
    .WithReference(database)
    .WithReference(mongoDatabase)
    .WaitFor(database)
    .WaitFor(mongoDatabase)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpHealthCheck("/health");


builder.AddViteApp("frontend", "../frontend")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("VITE_API_ORIGIN", api.GetEndpoint("http"));

builder.Build().Run();



static Func<ExecuteCommandContext, Task<ExecuteCommandResult>> ClearMongoDb(IResourceBuilder<MongoDBServerResource> mongoDatabase, string mongoDatabaseName)
{
    return async context =>
    {
        try
        {
            var connectionString = await mongoDatabase.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);
            await new MongoClient(connectionString).DropDatabaseAsync(mongoDatabaseName, context.CancellationToken);
            return CommandResults.Success("MongoDB database cleared.");
        }
        catch (Exception exception)
        {
            return CommandResults.Failure(exception);
        }
    };
}