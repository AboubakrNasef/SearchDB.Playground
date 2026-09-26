# Centralized Seed Commands Design

## Goal

Keep all demo and Faker seed/clear behavior in `SearchDB.SeedData`, and expose Aspire dashboard commands for both PostgreSQL and MongoDB. AppHost registers commands and delegates their work; infrastructure projects contain persistence mappings and search only.

## Architecture

- Move `DemoRecords`, `FakerRecords`, `PostgresSeed`, and `MongoSeed` into `SearchDB.SeedData`.
- `SearchDB.SeedData` references the PostgreSQL and MongoDB infrastructure projects to use their existing context/document mappings. Remove the reverse `SeedData` references from those infrastructure projects, avoiding a project cycle.
- Keep `WithCommand` registration in AppHost because Aspire owns resource command registration. The callbacks resolve connection strings, call the seed project APIs, and turn exceptions into command failures.
- Add explicit-start commands to seed and clear each provider. PostgreSQL clear empties its application tables while retaining the schema; MongoDB clear drops the configured database, matching the existing command behavior.
- Keep the initializer's existing startup/bootstrap flow, but call the centralized seeding APIs so startup and dashboard commands use the same data and logic.

## Data and behavior

- Standard seeds retain the four fixed products and three fixed orders.
- Faker mode adds the existing 100 repeatable products and 500 repeatable orders to both providers.
- Seed operations remain idempotent for the fixed Faker IDs and preserve existing records outside their known seed IDs.
- Dashboard command labels identify the provider and action. Faker mode follows `SeedData__UseFaker`, defaulting to enabled for the Aspire development host.

## Verification

- Build the full solution after changing project references.
- Keep the fixed-data integration fixtures' exact counts and ordering unchanged.
- Verify command seeding writes products and orders to each provider, and clear removes their application data without removing PostgreSQL schema.

## Scope

No API or frontend changes. No change to product/order schemas or search behavior.
