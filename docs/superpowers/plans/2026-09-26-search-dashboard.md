# Product and Order Search Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a React product/order search dashboard with separate PostgreSQL and MongoDB Atlas Search pages, implemented with Clean Architecture, Aspire, and OpenTelemetry.

**Architecture:** One ASP.NET Core API exposes two provider-specific endpoints over shared Application search contracts. PostgreSQL and MongoDB adapters use equivalent seeded data and return one normalized result shape; Aspire runs the API and local PostgreSQL, while MongoDB Atlas is configured externally.

**Tech Stack:** .NET 10 (existing target), Aspire 13.5.4 (existing AppHost SDK), ASP.NET Core, EF Core/Npgsql, MongoDB .NET/C# Driver with Atlas Search, React + TypeScript + Vite, OpenTelemetry OTLP, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-26-search-dashboard-design.md`

## Global Constraints

- Preserve the existing .NET 10 target and Aspire AppHost SDK 13.5.4 unless a compatibility issue requires a reviewed change.
- Domain and Application must not depend on database SDKs, EF Core, ASP.NET Core, or Aspire.
- Keep one API host with endpoints `GET /api/search/postgres` and `GET /api/search/mongo`.
- Both providers use the same search request, normalized response, filters, and equivalent logical seed data.
- Product search fields: SKU, name, category, description. Order search fields: order number, status, and order-item product SKU/name snapshots.
- MongoDB Atlas Search requires an Atlas deployment and configured Search indexes; do not imply that plain local MongoDB provides Atlas Search.
- Database integration tests use Testcontainers: PostgreSQL uses a disposable PostgreSQL container; Mongo search tests use MongoDB Atlas Local so MongoDB Search (`mongot`) runs in Docker.
- Never put raw search text, identifiers, or other user-entered/high-cardinality values in metric labels.
- Keep credentials out of source control. React can run with its own Vite dev server; Aspire owns the API and PostgreSQL lifecycle.

## Review Focus

- Empty query should provide a first page of browse results; pin this behavior in Application and provider tests.
- Page must be at least 1, page size 1–100, and query length at most 200 characters; pin invalid values to HTTP 400 in API tests.
- A date range with `createdFrom > createdTo` must return HTTP 400; test endpoint validation.
- Unsupported entity/filter combinations must return HTTP 400 rather than silently ignore filters; test both endpoint contracts.
- Missing Atlas configuration or unavailable Atlas Search index must produce a safe, actionable error and telemetry without connection secrets; cover configuration/error mapping and verify telemetry tags omit query text.

---

### Task 1: Domain entities and shared search contracts

**Files:**
- Modify: `Domain/Order.cs`
- Modify: `Domain/OrderStatus.cs`
- Create: `Domain/Product.cs`
- Create: `Domain/OrderItem.cs`
- Create: `SearchDB.Application/SearchDB.Application.csproj`
- Create: `SearchDB.Application/Search/SearchEntity.cs`
- Create: `SearchDB.Application/Search/SearchRequest.cs`
- Create: `SearchDB.Application/Search/SearchFilter.cs`
- Create: `SearchDB.Application/Search/SearchResult.cs`
- Create: `SearchDB.Application/Search/IPostgresSearch.cs`
- Create: `SearchDB.Application/Search/IMongoSearch.cs`
- Create: `SearchDB.Application/Search/SearchRequestValidator.cs`
- Create: `SearchDB.SeedData/SearchDB.SeedData.csproj`
- Create: `SearchDB.SeedData/DemoRecords.cs`
- Create: `Domain.Tests/Domain.Tests.csproj`
- Create: `Domain.Tests/ProductAndOrderTests.cs`
- Create: `SearchDB.Application.Tests/SearchRequestValidatorTests.cs`
- Create: `SearchDB.Application.Tests/SearchDB.Application.Tests.csproj`
- Modify: `SearchDB.slnx`

**Interfaces:**
- `SearchEntity`: enum values `Products`, `Orders`.
- `SearchRequest(SearchEntity Entity, string Query, int Page, int PageSize, SearchFilter Filters)`.
- `SearchFilter(string? Category = null, bool? Active = null, OrderStatus? Status = null, DateTimeOffset? CreatedFrom = null, DateTimeOffset? CreatedTo = null)`.
- `SearchResult(SearchEntity Entity, IReadOnlyList<SearchResultItem> Items, long? TotalCount, int Page, int PageSize, double DurationMilliseconds)`.
- `SearchResultItem(string Id, string PrimaryText, string SecondaryText, string? Category, string? Status, decimal? Amount, DateTimeOffset? CreatedAt, double Score)`.
- `SearchRequestValidator.Validate(SearchRequest request)` returns validation errors; empty query is valid, query length is at most 200, page is at least 1, page size is 1–100, and only filters applicable to the selected entity are accepted.
- `IPostgresSearch.SearchAsync(SearchRequest request, CancellationToken cancellationToken)` and `IMongoSearch.SearchAsync(...)` return `Task<SearchResult>`.
- Product carries ID, SKU, name, category, description, decimal unit price, currency, active flag. Order carries ID, order number, creation timestamp, status, and one or more items. Order item carries product ID, product SKU/name and unit-price snapshots, quantity, and calculated line total.

- [ ] **Step 1: Write failing domain tests** for required product values, positive order-item quantity, decimal line total, non-empty order items, and preserved item snapshots.
- [ ] **Step 2: Run the focused domain test** with `dotnet test Domain.Tests/Domain.Tests.csproj`; confirm the expected missing-type failures.
- [ ] **Step 3: Implement the domain model** in the files above, replacing the current title/date-based order shape while retaining `OrderStatus` values.
- [ ] **Step 4: Run the focused domain test** and confirm all invariant tests pass.
- [ ] **Step 5: Add Application contract tests** for default browse query handling, 200-character query maximum, page/page-size bounds, date range ordering, and filter/entity compatibility.
- [ ] **Step 6: Implement the shared search contracts and validator**. Use page default 1, page-size default 20, maximum page size 100, maximum query length 200; permit an empty query as browse mode.
- [ ] **Step 7: Run `dotnet test SearchDB.Application.Tests/SearchDB.Application.Tests.csproj`** and confirm validation cases pass.
- [ ] **Step 8: Add Domain, Application, SeedData, and test projects to `SearchDB.slnx`** and confirm `dotnet build SearchDB.slnx` succeeds.

### Task 2: PostgreSQL persistence, seed data, and search provider

**Files:**
- Create: `SearchDB.Infrastructure.Postgres/SearchDB.Infrastructure.Postgres.csproj`
- Create: `SearchDB.Infrastructure.Postgres/SearchDbContext.cs`
- Create: `SearchDB.Infrastructure.Postgres/Configurations/ProductConfiguration.cs`
- Create: `SearchDB.Infrastructure.Postgres/Configurations/OrderConfiguration.cs`
- Create: `SearchDB.Infrastructure.Postgres/Configurations/OrderItemConfiguration.cs`
- Create: `SearchDB.Infrastructure.Postgres/Search/PostgresSearch.cs`
- Create: `SearchDB.Infrastructure.Postgres.Tests/SearchDB.Infrastructure.Postgres.Tests.csproj`
- Create: `SearchDB.Infrastructure.Postgres.Tests/PostgresSearchTests.cs`
- Create: `SearchDB.Infrastructure.Postgres.Tests/PostgresContainerFixture.cs`
- Modify: `SearchDB.slnx`

**Interfaces:** Consumes Task 1 `SearchRequest`, `SearchResult`, `SearchResultItem`, Domain entities, and `IPostgresSearch`. Produces an `IPostgresSearch` implementation and relational schema. PostgreSQL integration tests run against a Testcontainers-managed PostgreSQL container.

- [ ] **Step 1: Write failing PostgreSQL provider tests** for product/order field search, shared filters, browse mode, stable paging, and empty results. Use Testcontainers.PostgreSql and a shared fixture that creates and disposes a disposable PostgreSQL container.
- [ ] **Step 2: Run the provider tests** and confirm failures show the missing provider/schema.
- [ ] **Step 3: Implement EF Core mappings** for Products, Orders, and OrderItems; enforce foreign keys, decimal precision, unique SKU/order number, and required order items.
- [ ] **Step 4: Add PostgreSQL full-text indexes and search queries** covering the spec fields; apply filters before paging and return relevance plus deterministic ID tie-break ordering.
- [ ] **Step 5: Implement the PostgreSQL seeder** to persist shared `SearchDB.SeedData.DemoRecords`, including order item snapshots.
- [ ] **Step 6: Create the PostgreSQL test schema with EF Core `EnsureCreatedAsync`, seed data, and assert schema and seed counts.** Never use or delete a developer's configured database in integration tests; schema migrations are deferred while this remains a seeded demo application.
- [ ] **Step 7: Run `dotnet test SearchDB.Infrastructure.Postgres.Tests/SearchDB.Infrastructure.Postgres.Tests.csproj`** and confirm all provider behavior passes.

### Task 3: MongoDB documents and Atlas Search provider

**Files:**
- Create: `SearchDB.Infrastructure.Mongo/SearchDB.Infrastructure.Mongo.csproj`
- Create: `SearchDB.Infrastructure.Mongo/Documents/ProductDocument.cs`
- Create: `SearchDB.Infrastructure.Mongo/Documents/OrderDocument.cs`
- Create: `SearchDB.Infrastructure.Mongo/Search/MongoSearch.cs`
- Create: `SearchDB.Infrastructure.Mongo/Search/Indexes/products-search-index.json`
- Create: `SearchDB.Infrastructure.Mongo/Search/Indexes/orders-search-index.json`
- Create: `SearchDB.Infrastructure.Mongo.Tests/SearchDB.Infrastructure.Mongo.Tests.csproj`
- Create: `SearchDB.Infrastructure.Mongo.Tests/MongoSearchTests.cs`
- Create: `SearchDB.Infrastructure.Mongo.Tests/AtlasLocalContainerFixture.cs`
- Modify: `SearchDB.slnx`

**Interfaces:** Consumes Task 1 shared contracts, Domain values, and `SearchDB.SeedData.DemoRecords`. Produces an `IMongoSearch` implementation and documented Atlas Search index definitions. Mongo documents store searchable order-item snapshots alongside order data. Integration tests use MongoDB Atlas Local in a Testcontainers-managed Docker container, not a remote account.

- [ ] **Step 1: Write failing Mongo provider tests** for product/order text fields, shared filters, browse mode, paging, stable ordering, and no-match behavior. Use a Testcontainers generic container with `mongodb/mongodb-atlas-local:8.3.2` so tests include MongoDB Search.
- [ ] **Step 2: Run provider tests** and confirm missing document mappings/search implementation failures.
- [ ] **Step 3: Implement Mongo document mappings and collections** and a seeder that writes the shared logical records also consumed by PostgreSQL.
- [ ] **Step 4: Add explicit Atlas Search index JSON** for product and order searchable/filterable fields; avoid relying on dynamic indexing.
- [ ] **Step 5: Implement `IMongoSearch` with Atlas `$search` aggregation** followed by compatible filters, normalized projection, count/page handling, and deterministic tie ordering.
- [ ] **Step 6: Add container-backed integration cases** that create Search indexes in Atlas Local, load shared demo records, and cover index-backed queries and data parity.
- [ ] **Step 7: Run Mongo provider tests** with Docker available and confirm all search contracts pass without a remote Atlas URI.

### Task 4: API endpoints, validation, error mapping, and telemetry

**Files:**
- Create: `SearchDB.Api/SearchDB.Api.csproj`
- Create: `SearchDB.Api/Program.cs`
- Create: `SearchDB.Api/Endpoints/SearchEndpoints.cs`
- Create: `SearchDB.Api/Errors/SearchExceptionHandler.cs`
- Create: `SearchDB.Api/Telemetry/SearchTelemetry.cs`
- Create: `SearchDB.Api.Tests/SearchDB.Api.Tests.csproj`
- Create: `SearchDB.Api.Tests/SearchEndpointTests.cs`
- Create: `SearchDB.Api.Tests/SearchTelemetryTests.cs`
- Modify: `SearchDB.slnx`

**Interfaces:** Consumes Task 1 request/response contracts, validators, `IPostgresSearch`, and `IMongoSearch`; produces `GET /api/search/postgres` and `GET /api/search/mongo`, both binding the same query parameters and response schema.

- [ ] **Step 1: Write failing endpoint tests** for both endpoint routes, provider dispatch, shared schema, validation responses, and safe provider failures.
- [ ] **Step 2: Implement both GET endpoints** with parameters `entity`, `query`, `page`, `pageSize`, `category`, `active`, `status`, `createdFrom`, and `createdTo`.
- [ ] **Step 3: Implement Problem Details mapping** so invalid inputs return 400, known provider/configuration/index-unavailable failures return 503, and unexpected failures return 500 without leaking connection details.
- [ ] **Step 4: Add OpenTelemetry search instrumentation** with an ActivitySource span and Meter instruments for duration histogram, result count, and error count. Add only backend and entity tags; never add query or entity IDs.
- [ ] **Step 5: Run API tests** and assert both routes dispatch to the correct provider and return the same response contract.
- [ ] **Step 6: Run telemetry tests** with an in-memory listener and assert provider/entity tags exist and query text does not appear in metric dimensions or activity tags.

### Task 5: Aspire orchestration and service configuration

**Files:**
- Modify: `SearchDB.AppHost/SearchDB.AppHost.csproj`
- Modify: `SearchDB.AppHost/AppHost.cs`
- Modify: `SearchDB.Api/Program.cs`
- Modify: `SearchDB.ServiceDefaults/Extensions.cs` only if existing defaults need a focused adjustment
- Create: `docs/development.md`

**Interfaces:** Consumes the API and PostgreSQL adapter from Tasks 2 and 4. Produces a local Aspire topology that starts PostgreSQL and API with connection wiring, health checks, traces, and metrics. Atlas connection settings remain external configuration.

- [ ] **Step 1: Add the Aspire PostgreSQL hosting integration** and model a `postgres` server plus `searchdb` database in `AppHost.cs`.
- [ ] **Step 2: Add API to AppHost** with a database reference and required configuration for both providers; configure Atlas URI from user secrets/environment only.
- [ ] **Step 3: Register EF Core, both search adapters, and ServiceDefaults in API composition** without leaking infrastructure references into Domain/Application.
- [ ] **Step 4: Add development instructions** for prerequisites, local Aspire startup, Atlas URI configuration, index setup, seed initialization, and React startup.
- [ ] **Step 5: Launch AppHost** and confirm PostgreSQL/API health, Aspire traces/metrics, and clear Atlas-not-configured behavior.

### Task 6: React dashboard with two provider pages

**Files:**
- Create: `frontend/package.json`
- Create: `frontend/tsconfig.json`
- Create: `frontend/vite.config.ts`
- Create: `frontend/src/main.tsx`
- Create: `frontend/src/App.tsx`
- Create: `frontend/src/api/search.ts`
- Create: `frontend/src/components/SearchForm.tsx`
- Create: `frontend/src/components/SearchResults.tsx`
- Create: `frontend/src/pages/PostgresSearchPage.tsx`
- Create: `frontend/src/pages/MongoSearchPage.tsx`
- Create: `frontend/src/pages/PostgresSearchPage.test.tsx`
- Create: `frontend/src/pages/MongoSearchPage.test.tsx`
- Create: `frontend/.env.example`

**Interfaces:** Consumes the Task 4 common response shape. `searchPostgres(request)` calls `/api/search/postgres`; `searchMongo(request)` calls `/api/search/mongo`. Routes `/postgres` and `/mongo` render explicit provider pages.

- [ ] **Step 1: Scaffold React + TypeScript + Vite** and install the smallest required router and component-test dependencies.
- [ ] **Step 2: Configure the Vite development proxy** to forward `/api` requests to the API origin from `VITE_API_ORIGIN`; document the origin in `.env.example`.
- [ ] **Step 3: Define typed request/response models** matching API query parameters and `SearchResult`/`SearchResultItem` JSON.
- [ ] **Step 4: Write frontend tests** verifying the PostgreSQL page calls only its endpoint and the MongoDB page calls only its endpoint with equivalent controls.
- [ ] **Step 5: Implement the shared search form and results view** with entity selector, query, applicable filters, paging, loading, empty, and error states.
- [ ] **Step 6: Implement `/postgres` and `/mongo` pages** with provider labels and endpoint-reported elapsed duration.
- [ ] **Step 7: Run frontend tests and production build**; confirm both routes work with configured API base URL.

### Task 7: End-to-end parity and operator handoff

**Files:**
- Create: `docs/search-comparison.md`
- Modify: `docs/development.md`
- Modify: provider/API test projects as needed

**Interfaces:** Consumes both provider endpoints, shared seed data, frontend pages, and Aspire telemetry. Produces a repeatable local demonstration and documented comparison limitations.

- [ ] **Step 1: Add parity scenarios** with the same query/filter cases executed against both endpoints, asserting compatible scope, IDs/logical content, paging, and response shape.
- [ ] **Step 2: Add the end-to-end smoke sequence** for an empty browse query, product keyword, order number, status filter, category filter, date filter, and no-match query.
- [ ] **Step 3: Document search behavior differences** such as tokenization/ranking and clarify that displayed endpoint duration is exploratory rather than a controlled benchmark.
- [ ] **Step 4: Document telemetry views** for traces and metrics, expected provider/entity labels, and confirmation that query text is excluded from dimensions.
- [ ] **Step 5: Run the full .NET test suite, frontend tests/build, and the documented local smoke sequence** with PostgreSQL and Atlas available; record any environment-dependent Atlas prerequisites.

## Execution notes

- Implement tasks in order because each establishes contracts required by later tasks.
- This plan contains seven tasks with significant interface dependencies. Native, sequential execution is recommended to keep provider contracts and API behavior consistent; perform one independent whole-plan review at the end.
- The workspace currently has no Git repository, so omit commit steps unless version control is initialized before implementation.
