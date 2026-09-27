# Centralized Seed Commands Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move seeding and clearing behavior into `SearchDB.SeedData` and expose Aspire commands for PostgreSQL and MongoDB.

**Architecture:** `SearchDB.SeedData` owns fixed/Faker records and both providers' seed operations, referencing the existing provider infrastructure for their persistence models. Infrastructure projects stop referencing SeedData. AppHost retains Aspire command registration and delegates to SeedData APIs.

**Tech Stack:** .NET 10, Aspire 13.5.4, EF Core/Npgsql, MongoDB.Driver, Bogus.

**Spec:** `docs/superpowers/specs/2026-09-27-centralized-seed-commands-design.md`

## Global Constraints

- Preserve four fixed products and three fixed orders as the default dataset.
- Faker mode adds 100 repeatable products and 500 repeatable orders to both providers.
- Preserve the `SeedData__UseFaker` setting, enabled by default in the Aspire development host.
- Keep PostgreSQL schema after clear; retain MongoDB clear's whole-database drop behavior.
- Do not change API contracts, schemas, or search behavior.

## Review Focus

- Seed project references both infrastructure projects while infrastructure projects have no reverse SeedData reference; verify the full solution builds.
- Seeded products are persisted before orders in both providers; preserve the current order.
- PostgreSQL clear handles order-item foreign keys and leaves tables/schema available.
- Mongo clear uses the configured database name, not a hard-coded name.
- Existing fixed-data integration fixtures retain their exact counts and ordering.

---

### Task 1: Move seed operations into SeedData

**Files:**
- Move: `SearchDB.Infrastructure.Postgres/Seed/PostgresSeed.cs` to `SearchDB.SeedData/Seed/PostgresSeed.cs`
- Move: `SearchDB.Infrastructure.Mongo/Seed/MongoSeed.cs` to `SearchDB.SeedData/Seed/MongoSeed.cs`
- Modify: `SearchDB.SeedData/SearchDB.SeedData.csproj`
- Modify: `SearchDB.Infrastructure.Postgres/SearchDB.Infrastructure.Postgres.csproj`
- Modify: `SearchDB.Infrastructure.Mongo/SearchDB.Infrastructure.Mongo.csproj`
- Modify: `SearchDB.Initialize/Program.cs`
- Modify: `SearchDB.Initialize/SearchDB.Initialize.csproj`
- Modify: `SearchDB.Infrastructure.Postgres.Tests/PostgresContainerFixture.cs`
- Modify: `SearchDB.Infrastructure.Mongo.Tests/AtlasLocalContainerFixture.cs`
- Modify: the two infrastructure test project files to reference `SearchDB.SeedData`

**Interfaces:**
- Keep `PostgresSeed.EnsureSeededAsync(SearchDbContext, CancellationToken, bool)` and `MongoSeed.EnsureSeededAsync(IMongoDatabase, CancellationToken, bool)` behavior and defaults.
- Add connection-string overloads for Postgres seed/clear so AppHost can delegate without constructing a context; add `MongoSeed.ClearAsync(IMongoDatabase, CancellationToken)` in SeedData.

- [ ] Move the two seed classes and update their namespace to `SearchDB.SeedData`.
- [ ] Add SeedData references to both infrastructure projects; remove SeedData references from the infrastructure project files to prevent a cycle.
- [ ] Update initializer and fixtures to use the relocated seeders; add direct SeedData references to test projects where required.
- [ ] Implement Postgres clear by removing order and product data in FK-safe order while retaining the schema; implement Mongo clear by dropping the supplied database.
- [ ] Build: `dotnet build SearchDB/SearchDB.slnx` (run from the workspace root). Expected: success with no project-reference cycle.

### Task 2: Add provider commands to AppHost

**Files:**
- Modify: `SearchDB.AppHost/AppHost.cs`
- Modify: `SearchDB.AppHost/SearchDB.AppHost.csproj` for direct SeedData access
- Modify: `SearchDB.Initialize/Program.cs` only if Task 1 did not complete the centralized calls

**Interfaces:**
- Aspire command callbacks call Postgres connection-string seed/clear overloads and `MongoSeed.EnsureSeededAsync` / `ClearAsync` from `SearchDB.SeedData`.
- Keep resource command registration in AppHost; each callback returns `CommandResults.Success` or `CommandResults.Failure`.

- [ ] Add explicit-start `SeedPostgresSampleData` and `ClearPostgresSampleData` commands to the initialize resource, resolving its `pg-searchdb` connection string for SeedData operations.
- [ ] Keep Mongo seed and clear commands on the initialize resource, rename their labels to identify MongoDB, and delegate to SeedData APIs.
- [ ] Make both seed commands pass the configured Faker setting; keep the existing whole-database confirmation on Mongo clear and add confirmation for PostgreSQL clear.
- [ ] Build: `dotnet build SearchDB/SearchDB.slnx`. Expected: all AppHost commands compile against SeedData and the project graph remains acyclic.

### Task 3: Document command usage

**Files:**
- Modify: `docs/development.md`

- [ ] Document the four Aspire dashboard commands, their clear behavior, and the `SeedData__UseFaker` switch.
- [ ] Confirm the documentation describes the fixed plus generated dataset accurately.

## Execution Notes

Do not add or run tests unless requested. Preserve the existing test fixture behavior and use solution builds as the verification check.
