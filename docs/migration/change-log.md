# Migration Change Log

All modifications, verification passes, and architectural updates are recorded here.

---

## [2026-07-11]

### Added (Phase 11 & 12 - Flight Booking Verification & Auditing Completion)
- Audited Flight Booking write slice against legacy Laravel financial behaviors (PayableAmount summation, passenger profit derivation, inhouse costing overrides, and user profit calculation).
- Implemented 7 new advanced integration tests in `FlightBookingWriteTests.cs` (idempotency key conflicts, simultaneous duplicate requests, wallet concurrent overspend, salesman profit concurrent updates, failure rollback verification, pre-cancellation token propagation, and mixed passenger types costing).
- Added recovery logic to `CreateFlightBookingHandler` allowing retry of failed idempotency keys by resetting their state from `"Failed"` to `"InProgress"`.
- Added test-only rollback sentinel check (`TriggerRollbackException`) in passenger loop to verify transactional rollback.
- Wrote 11 documentation artifacts covering the gap audit, test coverage, financial model, security input boundaries, concurrency protections, and frontend accessibility/responsiveness.
- Verified total backend tests passed increased from 107 to 114 (108 integration tests).
- Verified that Vitest unit checks (51 tests) and `svelte-check` (0 errors, 0 warnings) pass cleanly.
- Implemented Hotel Booking vertical slice (backend write request contracts, controller mapping, transactional handler, unit tests, frontend helper, list table, and creation form).
- Implemented Flight Booking vertical slice (backend write contracts, controller endpoint mapping, transactional handler with dynamic passenger profit and price aggregation, unit tests, integration tests, Svelte list page, and responsive passenger-array creation form).
- Configured customer Head ownership validation ensuring salesmen can only book against heads they created.
- Resolved CS8604 and CS8602 compiler nullability warnings in handler codes.
- Regenerated OpenAPI contracts mapping the newly implemented write models.

## [2026-07-10]

### Added (Phase 10 - Sales Read UI Vertical Slice)
- Implemented Dashboard aggregates UI displaying live summary sales, gross profits, cash in hand, and active accounts.
- Added dynamic HTML progress bar charts representing service type distribution of total bookings.
- Built responsive green SVG daily sales trend visual with contract gap warning disclosures.
- Created Heads customer list page displaying paginated datatables, click-sorting, and debounced search filters synced with browser URL search params.
- Created Head details route (`/heads/[headId]`) displaying detailed profile columns, paginated Ledger tab, and Outstanding Bookings mock data overlays.
- Created `DevMockAuthHandler` backend custom authentication scheme registered local-only under ASP.NET Core Development environment.
- Configured frontend `session.ts` and `client.ts` to automatically inject `dev-salesman-token` for local queries and fail-closed authentication in production builds.
- Created unit tests verifying URL search parameter serializers, API request constructors, and auth environment gating.
- Expanded Playwright E2E browser tests to verify heads listing transition, sorting, and tab page navigations.

### Added (Phase 9 - Sales Frontend Foundation)
- Stopped/Deferred backend Booking Write phase (wallet/CashInHand mutations, ledgers, write controllers).
- Configured SvelteKit project with Svelte 5 (Runes), TypeScript strict mode, and shadcn-svelte.
- Configured Tailwind CSS v4 with Vite native plugin integration.
- Created CSS variable design tokens supporting multi-portal styling configurations (Green theme active for Sales).
- Implemented centralized API Client (`client.ts`) and RFC 9457 `ProblemDetails` parser (`problem-details.ts`).
- Implemented client auth boundaries (`session.ts`) and permission-gated navigation configs.
- Built responsive layout shell with desktop sidebar, mobile overlays, breadcrumbs, and user profiles.
- Configured dev CORS policy in backend `Program.cs` for local SvelteKit origins.
- Created 20 Vitest unit tests and 4 Playwright browser E2E smoke checks.

### Added (Phase 7B - Cross-Provider Alignment & Security Updates)
- Created **[cross-provider-migration-decision.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/architecture/cross-provider-migration-decision.md)** establishing MySQL to SQL Server decision.
- Created **[mysql-source-schema-profile.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/migration/mysql-source-schema-profile.md)** documenting static database profile findings.
- Created **[canonical-schema-inventory.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/migration/canonical-schema-inventory.md)** mapping explicit columns.
- Created **[financial-storage-strategy.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/architecture/financial-storage-strategy.md)** mapping standard BHD currencies precision.
- Created **[date-time-strategy.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/architecture/date-time-strategy.md)** tracking local timezone conversions.
- Created **[status-value-inventory.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/migration/status-value-inventory.md)** mapping database enums to constants.
- Created **[relationship-fidelity-matrix.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/migration/relationship-fidelity-matrix.md)** tracing cascade constraints.
- Created **[sql-server-schema-deployment-strategy.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/architecture/sql-server-schema-deployment-strategy.md)** detailing empty target database configurations.
- Created **[mysql-to-sqlserver-data-migration-strategy.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/architecture/mysql-to-sqlserver-data-migration-strategy.md)** specifying dependencies load sequences.
- Created **[financial-reconciliation-strategy.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/architecture/financial-reconciliation-strategy.md)** listing ledger integrity query scripts.
- Created **[data-migration-validation-plan.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/migration/data-migration-validation-plan.md)** scheduling validations post load.
- Created **[secrets-management.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/security/secrets-management.md)** defining credential management guidelines.
- Created **[database-test-strategy.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/testing/database-test-strategy.md)** classifying testing tiers.
- Configured transient execution retries via `EnableRetryOnFailure` globally in `DependencyInjection.cs`.
- Decoupled `DatabaseConnectivityTests` from hardcoded connection credentials.
- Updated database settings in `appsettings.Development.json` with safe placeholders.
- Updated `DatabaseMappingTests` to assert exact financial precision profiles.
- Corrected status classifications in `progress.md`.

### Added (Phase 7 - Database Mapping & Persistence)
- Created **23 Domain entities** inside [Entities/](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/src/TexasPortal.Domain/Entities/) mapping all Sales Portal tables.
- Created [ISalesPortalDbContext](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/src/TexasPortal.Application/Persistence/ISalesPortalDbContext.cs) defining boundary query surfaces.
- Created [SalesPortalDbContext](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/src/TexasPortal.Infrastructure/Persistence/SalesPortalDbContext.cs) concrete DbContext.
- Created [EntityConfigurations.cs](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/src/TexasPortal.Infrastructure/Persistence/Configurations/EntityConfigurations.cs) wrapping mapping details, decimals, and foreign keys.
- Created [DependencyInjection.cs](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/src/TexasPortal.Infrastructure/DependencyInjection.cs) registering SQL Server persistence.
- Created [SalesPortalDbContextFactory](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/src/TexasPortal.Infrastructure/Persistence/SalesPortalDbContextFactory.cs) for CLI migrations.
- Created [DatabaseMappingTests.cs](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/tests/TexasPortal.IntegrationTests/DatabaseMappingTests.cs) verifying schema configuration constraints.
- Created [DatabaseConnectivityTests.cs](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/tests/TexasPortal.IntegrationTests/DatabaseConnectivityTests.cs) verifying remote connectivity and generated SQL string translations.
- Created ADR-007 inside [docs/architecture/decisions/](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/architecture/decisions/).
- Created [database-resiliency-strategy.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/architecture/database-resiliency-strategy.md) setting retry guidelines.
- Created [legacy-database-baseline-strategy.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/architecture/legacy-database-baseline-strategy.md) detailing EF baseline creation.
- Created [entity-inventory.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/migration/entity-inventory.md).
- Created [index-inventory.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/migration/index-inventory.md).
- Created [schema-discrepancies.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/migration/schema-discrepancies.md).
- Created [ef-generated-sql-review.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/migration/ef-generated-sql-review.md).

### Added (Initial Setup)
- Created [repository-structure.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/architecture/repository-structure.md) mapping project file layout and .NET 9.0 environment criteria.
- Created [integration-consistency-strategy.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/architecture/integration-consistency-strategy.md) detailing Transactional Outbox and Idempotency key workflows.
- Created [password-migration-strategy.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/architecture/password-migration-strategy.md) resolving legacy BCrypt authentication requirements.
- Created [financial-precision-and-rounding.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/migration/financial-precision-and-rounding.md) outlining precision mapping rules.
- Created [ef-core-query-strategy.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/architecture/ef-core-query-strategy.md) setting tracking and projection guidelines.
- Created [payment-allocation-algorithm.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/migration/payment-allocation-algorithm.md) defining outstanding invoice allocation steps.
- Created ADR-001 through ADR-006 inside [docs/architecture/decisions/](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/architecture/decisions/).
- Created [ArchitectureTests.cs](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/tests/TexasPortal.ArchitectureTests/ArchitectureTests.cs) asserting project boundary checks.

### Modified
- Updated [technical-debt-analysis.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/migration/technical-debt-analysis.md) to correct query terminology.
- Updated [transaction-analysis.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/migration/transaction-analysis.md) mapping synchronous HTTP calls inside transaction blocks.
- Updated [concurrency-risks.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/migration/concurrency-risks.md) evaluating atomic UPDATEs vs RMW scopes.
- Updated [progress.md](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/migration/progress.md) to capture correct phase statuses.
