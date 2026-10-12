# EF Core Query Strategy

This document details the database query strategy, execution policies, and performance monitoring standards for EF Core.

---

## 1. Change Tracking Policies

EF Core provides three tracking modes, which must be selected according to the use case boundary:

1. **Normal Tracking (`Default`)**:
   - *When to Use*: Use for transactional write workflows (e.g. creating bookings, updating ledger balances, checking out receipts). Change tracking allows modifying entity fields and committing all updates via a single `SaveChangesAsync()` call.
2. **No-Tracking (`AsNoTracking()`)**:
   - *When to Use*: Use for read-only queries (e.g. dashboard lists, head search lookup, detailed booking history views). Disabling tracking avoids memory overhead and improves performance.
3. **No-Tracking with Identity Resolution (`AsNoTrackingWithIdentityResolution()`)**:
   - *When to Use*: Use in read-only queries that fetch complex nested graphs where the same entity is referenced multiple times. This preserves object relationships while avoiding change tracker overhead.

---

## 2. Query Design Standards

- **Projection-First Design**:
  - Always project queries directly to contract DTOs using `.Select()` instead of eager loading whole entities via `.Include()` whenever the data is only used for read-only API responses.
  - *Example*:
    ```csharp
    var summary = await db.Heads
        .AsNoTracking()
        .Where(h => h.UserId == userId)
        .Select(h => new HeadDto { Name = h.Name, Balance = h.Balance })
        .ToListAsync(cancellationToken);
    ```
- **Lazy Loading**:
  - Lazy loading is **disabled**. It introduces high risk of N+1 database round trips. All nested entities must be explicitly loaded via `.Include()` or projected using `.Select()`.
- **Split Queries (`AsSplitQuery()`)**:
  - Only use split queries for complex multi-join queries where a single query would cause a cartesian explosion. Split queries must be evaluated on a case-by-case basis.
- **Query Tagging**:
  - Append `.TagWith()` to all significant queries to identify their source use case in SQL Server profiler logs.
  - *Example*:
    ```csharp
    var stats = await db.Receipts
        .TagWith("DashboardController:GetRevenueChartData")
        .Where(...)
        .ToListAsync();
    ```

---

## 3. Query Diagnostics & Observability

- **SQL Logging**:
  - In `Development`, configure logging to output all generated SQL to console diagnostics.
- **Slow Query Monitoring**:
  - Set up warning notifications or structured logs for any query execution duration exceeding `200ms` (measured via custom middleware or EF Core interceptors).
- **Execution Validation**:
  - In integration tests, configure EF Core to throw an exception if a query cannot be translated to SQL, preventing unexpected client-side evaluations.
