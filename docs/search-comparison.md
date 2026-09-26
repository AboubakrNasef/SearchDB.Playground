# Search provider comparison

The dashboard sends equivalent searches to separate endpoints: `/api/search/postgres` and `/api/search/mongo`. Both use the shared demo catalog and orders, request filters, paging defaults, and normalized response fields. PostgreSQL uses English full-text vectors and rank; MongoDB uses Atlas Search text indexes and search score.

## Smoke sequence

Run Aspire and the React development server as described in [development.md](development.md). PostgreSQL should be ready with shared demo records. Configure MongoDB Atlas and wait for both Atlas Search indexes to become queryable before comparing the Mongo page.

On each provider page, try:

1. Leave the query empty and browse Products.
2. Search Products for `keyboard`, then narrow by category or active status.
3. Switch to Orders and search for an order number such as `ORD-1001`.
4. Clear the query and filter Orders by Pending status.
5. Apply an order creation date range.
6. Search for a phrase that has no matching demo record.
7. Use Next and Previous when the result set spans multiple pages.

The UI shows each API provider's elapsed duration. Treat this as an exploratory comparison only: it includes database-side processing and API work, but not browser network time or a controlled warm/cold benchmark. Tokenization, stemming, scoring scales, and result relevance differ between PostgreSQL and Atlas Search, so identical documents do not imply identical ranks.

## OpenTelemetry

The API emits `search.execute` spans with a child `db.search` span and the `search.duration`, `search.results`, and `search.errors` instruments. Provider and entity are the only search dimensions. Search text and record IDs are excluded. Aspire's dashboard displays the API traces and metrics when the app is run through AppHost.

## Container-backed integration tests

`dotnet test SearchDB.slnx` runs the PostgreSQL adapter against a disposable `postgres:16-alpine` Testcontainer and the Atlas Search adapter against `mongodb/mongodb-atlas-local:8.3.2`, which includes `mongot`. Docker must be available. These verify local container behavior; Atlas Local is not a substitute for a production-cluster benchmark.
