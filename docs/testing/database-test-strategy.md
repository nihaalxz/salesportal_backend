# Database Test Strategy

This document classifies and structures database mapping, schema, and connectivity tests in the Texas Portal test suites.

---

## 1. Database Test Categories

To maintain test execution speed and reliability, database tests are classified into distinct tiers:

### Tier 1: EF Model Metadata Tests
- **Purpose**: Verify mapping declarations (table names, keys, precision, max lengths, shadow FK properties) in-memory using EF Core's builder metadata.
- **Database Required**: **None** (Fast, offline execution).
- **Execution**: Run in CI and local environments.
- **Location**: [DatabaseMappingTests.cs](file:///d:/freelanceprojects/TexasPortalsDotNet\texas_sales_portal_backend\texas_sales_portal_backend\tests\TexasPortal.IntegrationTests\DatabaseMappingTests.cs).

### Tier 2: Database Connectivity Tests
- **Purpose**: Verify that the target database engine is reachable and connection strings are valid.
- **Database Required**: Target SQL Server Database (online connection required).
- **Skip Policy**: Automatically skipped if `TEXAS_PORTAL_CONNECTION_STRING` environment variable is not configured.
- **Location**: [DatabaseConnectivityTests.cs](file:///d:/freelanceprojects/TexasPortalsDotNet\texas_sales_portal_backend\texas_sales_portal_backend\tests\TexasPortal.IntegrationTests\DatabaseConnectivityTests.cs).

### Tier 3: Target Schema & Migration Validation Tests
- **Purpose**: Verify that DDL schema constraints and indexes match expectation.
- **Database Required**: SQL Server Staging Database (populated with migrated schema).
- **Execution**: Executed manually or in deployment pipelines post-migration.

---

## 2. Test Execution Policies

- **CI/CD Integration**: Only Tier 1 (Metadata) tests run by default in CI to avoid dependency on database instances.
- **Local Development**: Developer runs `dotnet test` which executes Tier 1 and skips Tier 2 if connection strings are unconfigured in system environment variables.
