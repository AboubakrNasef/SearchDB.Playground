# Search Dashboard with PostgreSQL and MongoDB Atlas Search

## Purpose

Build a small, realistic product and order dashboard that demonstrates Clean Architecture with .NET and Aspire, while allowing a user to compare PostgreSQL search with MongoDB Atlas Search. The dashboard has one page per database. Each page calls its own API endpoint and uses the same search inputs and equivalent seeded data.

## Current project

The solution currently contains a .NET 10 `Domain` project with a basic `Order` and `OrderStatus`, an Aspire `AppHost`, and `ServiceDefaults`. It does not yet contain an API, persistence projects, or a React frontend. The existing order shape is a starting point and can be adjusted to support order items and searchable records.

## Goals

- Model products, orders, and order items with sensible business rules.
- Keep business and application logic independent of database technologies.
- Provide separate PostgreSQL and MongoDB Atlas Search endpoints.
- Provide two frontend pages with consistent search controls and result presentation.
- Use equivalent seed data and search fields so users can compare both implementations.
- Use Aspire to run the local application and PostgreSQL, and to view development telemetry.
- Use OpenTelemetry traces and metrics to observe each search path.

## Out of scope

- Keeping PostgreSQL and MongoDB synchronized as live replicas.
- Authentication, payments, inventory reservation, or order fulfillment workflows.
- A statistically rigorous performance benchmark. Telemetry is for observing request behavior; controlled benchmarking can be added separately.
- A production deployment architecture.

## Architecture

Use a Clean Architecture dependency direction:

1. **Domain** contains `Product`, `Order`, `OrderItem`, and `OrderStatus`, with domain invariants and no framework dependencies.
2. **Application** contains search use cases, request/response models, and interfaces for the two search implementations. It depends on Domain, not on database SDKs.
3. **Infrastructure** contains PostgreSQL and MongoDB implementations. These can be separate projects or clearly separated modules, provided database-specific packages and models do not leak into Domain or Application.
4. **API** exposes the two distinct search endpoints and composes the application and infrastructure services.
5. **React frontend** has two pages: PostgreSQL Search and MongoDB Search. Both pages use the same controls and result shape, but call different endpoints.
6. **Aspire AppHost and ServiceDefaults** orchestrate local services and shared health, discovery, resilience, and OpenTelemetry configuration.

The API is a single deployable service initially. Separate endpoints do not require separate API services.

## Domain and data shape

- A **Product** has an ID, SKU, name, category, description, unit price, currency, and active flag.
- An **Order** has an ID, order number, creation timestamp, status, and one or more order items.
- An **OrderItem** references a product and stores the product name/SKU and unit price as an order-time snapshot, plus quantity. Historical order details must not change when the catalog product is edited.
- Order status starts with the existing values: Pending, InProgress, Completed, and Cancelled.
- Prices use decimal precision and an explicit currency. An order item total is quantity multiplied by its snapshotted unit price.

PostgreSQL uses relational tables and foreign keys for catalog products, orders, and order items. MongoDB stores equivalent logical records in collections/documents suited to its search indexes; denormalizing product search fields into order items is acceptable to support searching order contents. Both stores receive the same logical seed records.

## Search and API behavior

Expose two endpoints with a shared contract:

- `GET /api/search/postgres`
- `GET /api/search/mongo`

The request accepts an entity scope (`products` or `orders`), a text query, paging, and a small shared set of filters. Product searches cover SKU, name, category, and description. Order searches cover order number, status, and the product SKU/name snapshots on its items. Shared filters include product category/active status and order status/date range, as applicable to the selected scope.

Both endpoints return the same response shape: entity scope, normalized items, total match count when supported, page information, and elapsed search duration. Results are ordered by relevance where available, with a stable secondary ordering for ties. Each backend may use its native search syntax and ranking, but the UI and filters remain consistent. Differences in tokenization, ranking, and supported query syntax must be documented rather than hidden.

PostgreSQL uses its native full-text search capabilities and indexes over the selected fields. MongoDB uses Atlas Search indexes and queries. MongoDB Atlas Search requires a reachable Atlas deployment configured for the application; local Aspire orchestration must document this external prerequisite. PostgreSQL should be runnable locally through Aspire.

## Frontend

The React app has two routes/pages, one for PostgreSQL and one for MongoDB Atlas Search. Each page offers the same entity selector, query input, applicable filters, paging, loading/error states, and result fields. The page identifies which backend it uses and displays the endpoint-reported duration for exploratory comparison. Shared presentation components are appropriate, while each page remains explicit about its backend.

## Observability

Use the existing Aspire ServiceDefaults OpenTelemetry setup and OTLP export when configured. Instrument each search request with an HTTP trace and a database-operation span so the API-to-database path is visible. Record a search-duration histogram, result count, and error count, tagged with backend and entity scope. Avoid search text, identifiers, or other high-cardinality/user-entered values in metric labels. Aspire's dashboard is the local viewing surface for traces and metrics.

The implementation should make telemetry behavior comparable across endpoints, while documenting any instrumentation differences between PostgreSQL and the MongoDB driver. Search duration returned to the UI is a convenience measure and is not a substitute for a controlled benchmark.

## Error handling and configuration

- Invalid scope, paging, or filters return a client error with a useful problem response.
- Database/search failures return a consistent server error response and are recorded in traces/metrics without exposing credentials or connection details.
- Connection strings and Atlas configuration come from normal .NET configuration/environment variables; secrets are not committed.
- Aspire local startup should clearly report the services it can start and the Atlas configuration required for the MongoDB page.

## Validation approach

- Unit-level checks cover domain invariants and application search request handling.
- Integration checks verify each endpoint against its respective database and equivalent seed data.
- Contract checks ensure both providers return the same response shape and honor shared filters.
- A frontend check confirms each page calls its matching endpoint and displays loading, empty, error, and result states.
- An observability check confirms traces and search metrics distinguish provider and scope without recording raw query text.

## Delivery sequence

1. Refine the starter domain model and establish project references and dependency direction.
2. Define shared search contracts and seed records.
3. Add PostgreSQL persistence, indexes, and the PostgreSQL search endpoint.
4. Add MongoDB persistence, Atlas Search indexes, and the MongoDB search endpoint.
5. Add comparable seed/setup documentation and API error behavior.
6. Add the two React pages using the shared result contract.
7. Configure Aspire orchestration and OpenTelemetry for both search paths.
8. Validate data parity, endpoint contracts, frontend behavior, and telemetry.

## Assumptions to confirm during implementation planning

- The initial product/order dataset is seeded demo data, not user-entered production data.
- PostgreSQL is the local baseline; MongoDB Atlas is configured as an external service.
- Search comparison is exploratory in the dashboard; a dedicated benchmark harness is a later extension.
