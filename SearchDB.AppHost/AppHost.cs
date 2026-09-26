using Microsoft.AspNetCore.Identity;

var builder = DistributedApplication.CreateBuilder(args);

var name = builder.AddParameter("database-user","user" ,secret: false);
var password = builder.AddParameter("database-password","password");
var postgres = builder.AddPostgres("postgres",name,password, 5454)
    .WithVolume("pdVol", "/var/postgresql/").WithPgAdmin();

var database = postgres.AddDatabase("pg-searchdb");
var mongoDb = builder.AddMongoDB("mongo",27029,name,password)
    .WithDataVolume();

var mongoDatabase = mongoDb.AddDatabase("mongo-searchdb").WithCommand("SeedSampleData", "SeedSampleData", () => {});

var api = builder.AddProject<Projects.SearchDB_Api>("api")
    .WithReference(database)
    .WithReference(mongoDatabase)
    .WaitFor(database)
    .WaitFor(mongoDatabase)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.SearchDB_Initialize>("initialize")
    .WithReference(database)
    .WithReference(mongoDatabase)
    .WaitFor(database)
    .WaitFor(mongoDatabase)
    .WithEnvironment("Mongo__Database", builder.Configuration["Mongo:Database"] ?? "searchdb")
    .WithExplicitStart();


builder.AddViteApp("frontend", "../frontend")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("VITE_API_ORIGIN", api.GetEndpoint("http"));

builder.Build().Run();
