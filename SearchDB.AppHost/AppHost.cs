var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var database = postgres.AddDatabase("searchdb");

var api = builder.AddProject<Projects.SearchDB_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("Database__InitializeOnStartup", "true")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpHealthCheck("/health");

var mongoConnectionString = builder.Configuration["Mongo:ConnectionString"];
if (!string.IsNullOrWhiteSpace(mongoConnectionString))
    api.WithEnvironment("Mongo__ConnectionString", mongoConnectionString);

var mongoDatabase = builder.Configuration["Mongo:Database"];
if (!string.IsNullOrWhiteSpace(mongoDatabase))
    api.WithEnvironment("Mongo__Database", mongoDatabase);

builder.Build().Run();
