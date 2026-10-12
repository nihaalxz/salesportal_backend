# SQL Server Test Concurrency Review

## 1. Root Cause
When running the xUnit integration test suite, individual test classes execute in parallel by default. Both classes `SalesApiHttpTests` and `SalesAuthorizationRoleTests` utilize `SalesApiTestFactory` and call `InitializeDatabaseAsync()` during their test lifecycle initialization. This initialization drops and recreates/migrates the database `TexasPortal_HTTP_Tests` on SQL Server.
Because they executed in parallel, multiple tests concurrently tried to drop, migrate, or insert seeds into the same catalog. This resulted in SQL connection timeouts, database-already-exists errors, database-does-not-exist errors, and foreign key conflicts (`FK_heads_users_created_by` merge constraint conflict) due to concurrent deletes.

## 2. Shared Resource
- **Database Catalog**: `TexasPortal_HTTP_Tests` on `localhost\SQLEXPRESS` (defined in `SalesApiTestFactory.cs`).

## 3. Affected Test Classes
- `TexasPortal.IntegrationTests.SalesApiHttpTests`
- `TexasPortal.IntegrationTests.SalesAuthorizationRoleTests`

## 4. Fix
Assigned both classes to a shared xUnit test collection:
- Class `SalesApiHttpTests` was decorated with `[Collection("SalesApi")]`.
- Class `SalesAuthorizationRoleTests` was decorated with `[Collection("SalesApi")]`.

By placing them in the same named collection, xUnit guarantees that the tests in both classes execute sequentially relative to each other, eliminating parallel database setup/teardown collisions.

## 5. Why Fix Is Correct
xUnit ensures sequential execution for all test classes sharing the same Named Collection. This serializes all calls to `db.Database.EnsureDeletedAsync()` and `db.Database.MigrateAsync()` targeting `TexasPortal_HTTP_Tests`.
Since each test method within these classes runs on a fresh database instance (cleared and seeded fresh during its `InitializeAsync()`), running sequentially guarantees that no other execution drops or inserts rows while a test is executing. 

## 6. Remaining Risks
- **Test Execution Speed**: Serializing execution increases the total run time of the integration suite (sequentially dropping/recreating the DB 11 times instead of in parallel). However, this is necessary for transactional database reliability against a single SQL Server instance.
- **State Leakage**: Since every test method invokes `InitializeDatabaseAsync()` sequentially, the database is dropped and seeded fresh. Therefore, there is zero risk of state leakage.

## 7. Repeated Run Results
The `SalesApi` test collection (consisting of 11 tests across both classes) was executed 5 times consecutively.
- Run 1: 11 Passed, 0 Failed (Duration: 6s)
- Run 2: 11 Passed, 0 Failed (Duration: 6s)
- Run 3: 11 Passed, 0 Failed (Duration: 6s)
- Run 4: 11 Passed, 0 Failed (Duration: 6s)
- Run 5: 11 Passed, 0 Failed (Duration: 6s)

The entire suite of 71 integration tests was executed, and all tests passed successfully with 0 failures.
