# SDD ledger — plan: docs/superpowers/plans/2026-09-26-search-dashboard.md

Pre-flight: Task 1 produces SearchRequest/SearchResult and IPostgresSearch/IMongoSearch; Tasks 2–4 consume those exact shared contracts. Match confirmed.
Pre-flight: Task 1 DemoRecords is consumed by Tasks 2–3; both adapters will use the shared seed project. Match confirmed.
Pre-flight: Task 4 endpoints dispatch the provider interfaces from Task 1; Task 5 registers both adapters in the API. Match confirmed.
Pre-flight: Task 4 returns SearchResult to both UI pages in Task 6; the TS models must mirror its JSON fields. Match confirmed.
Ruling: The .NET SDK installed is 11.0.100-rc.1 while all existing projects target net10.0 — preserve the committed target and use installed SDK if it supports net10 targeting; otherwise record the precise missing-targeting-pack condition rather than silently retargeting.
Task 1: complete (no commits; tests: dotnet test SearchDB.slnx --no-restore → 9/9 pass; build: dotnet build SearchDB.slnx --no-restore → 0 errors).
Task 2: Ruling: use EF Core EnsureCreatedAsync for the demo schema instead of committing migration artifacts — the approved spec requires a realistic seeded app but not schema evolution, and EnsureCreated is sufficient for this Docker-backed demo — cost if wrong: schema changes will require a fresh local database until migrations are added.
Task 2: complete (no commits; tests: dotnet test SearchDB.Infrastructure.Postgres.Tests/SearchDB.Infrastructure.Postgres.Tests.csproj --no-restore → 2/2 pass against disposable PostgreSQL Testcontainer).
Task 3: Ruling: use the official MongoDB Atlas Local 8.3.2 image in a Testcontainers generic container — this packages mongod and mongot for actual `$search` tests without a remote Atlas account — cost if wrong: behavior can still differ from a managed Atlas cluster.
Task 3: complete (no commits; tests: dotnet test SearchDB.Infrastructure.Mongo.Tests/SearchDB.Infrastructure.Mongo.Tests.csproj --no-restore → 3/3 pass against Atlas Local Testcontainer).
Task 4: complete (no commits; tests: dotnet test SearchDB.Api.Tests/SearchDB.Api.Tests.csproj --no-restore → 8/8 pass; both endpoint dispatch and telemetry privacy/child spans covered).
Task 5: complete (Aspire PostgreSQL + API orchestration, startup seeding/indexes, configuration, health checks, and development setup docs; AppHost previously launched with Aspire dashboard and PostgreSQL/API services).
Task 6: complete (React + TypeScript + Vite with explicit PostgreSQL and Mongo pages, matching provider endpoints, entity-specific filters, browse/search, paging, loading/empty/error states, and elapsed time; frontend tests 2/2 and TypeScript/Vite production build pass).
Task 7: complete (shared Postgres/Mongo seed parity asserted for deterministic product browse; smoke and telemetry comparison documented; full .NET suite 25/25 passes, including Testcontainers PostgreSQL and Atlas Local; frontend tests/build pass).
Ruling: Postgres integration tests truncate only their disposable Testcontainers database before seeding shared DemoRecords. This keeps test cases isolated and proves the same four-product baseline as MongoDB; no developer database is targeted.
