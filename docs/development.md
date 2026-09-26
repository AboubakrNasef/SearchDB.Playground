# Local development

## Prerequisites

- .NET 10 SDK and the Aspire CLI 13.5.4 bundle.
- Docker Engine or Docker Desktop. Aspire starts PostgreSQL in a container; the provider integration tests also start disposable PostgreSQL and MongoDB containers.
- Node.js LTS with npm for the React development server.
- A MongoDB Atlas connection string to use the app's Mongo page. The Mongo integration tests use MongoDB Atlas Local in Docker and do not need a remote Atlas account.

## Start the .NET services

From the solution directory, run:

```powershell
dotnet run --project SearchDB.AppHost
```

Aspire starts the API and a persistent local PostgreSQL database. On first API startup in Development, the API creates the schema and loads shared demo products and orders. PostgreSQL search is available without MongoDB configuration.

## Configure MongoDB Atlas for the app

Store credentials in AppHost user secrets (never in `appsettings.json`):

```powershell
dotnet user-secrets set "Mongo:ConnectionString" "mongodb+srv://<user>:<password>@<cluster>/" --project SearchDB.AppHost
dotnet user-secrets set "Mongo:Database" "searchdb" --project SearchDB.AppHost
```

When configured, the Development API seeds the shared demo documents and creates the `products-search` and `orders-search` indexes from the JSON definitions under `SearchDB.Infrastructure.Mongo/Search/Indexes`. MongoDB Atlas Search indexes may take time to become queryable after creation. If Atlas is unavailable or cannot create an index, the API still starts so the PostgreSQL page remains usable; Mongo search reports the provider as unavailable.

## Start the React dashboard

In a second terminal:

```powershell
cd frontend
npm install
Copy-Item .env.example .env.local
npm run dev
```

The Vite development server proxies `/api` to `http://localhost:63253`, the API's local HTTP address when run directly. When using Aspire, copy the API HTTP URL shown in the Aspire dashboard into `VITE_API_ORIGIN` in `.env.local` because Aspire may assign a dynamic port. Open the URL printed by Vite and use the PostgreSQL or MongoDB Atlas item in the sidebar. Each page calls only its matching API endpoint; both pages support product/order browse and search, filters, pagination, and provider duration.

The frontend scripts are `npm test` and `npm run build`. The displayed API duration is the provider request time, not a browser round trip or a controlled performance benchmark.

## Run integration tests

```powershell
dotnet test SearchDB.slnx
```

The PostgreSQL test suite starts `postgres:16-alpine` with Testcontainers. The MongoDB Search test suite starts `mongodb/mongodb-atlas-local:8.3.2`; it bundles MongoDB and MongoDB Search for local development/testing. Docker must be running. The first Mongo test run downloads the image (roughly 615 MB); later runs reuse the cached image.
