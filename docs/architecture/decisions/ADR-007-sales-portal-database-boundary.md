# ADR-007: Sales Portal Database Abstraction and Boundary

## Status
Proposed (Under Review)

## Context
Multiple portals share the same database. We must decide how application layer code queries and writes data, balancing coupling to EF Core vs. architectural boundary overhead.

*Correction*: The previous claim that EF Core tracking is a query optimization strategy for read-only workloads is incorrect. Change tracking overhead actually increases CPU and memory consumption. For read-only workloads, disabling tracking (`AsNoTracking()`) is the correct optimization.

---

## 1. Options Evaluated

### Option 1: Application-layer `ISalesPortalDbContext` exposing `DbSet<T>`
- **Pros**: Mockable interface, separates configuration from use cases.
- **Cons**: Exposes EF Core types to Application layer, mirroring DbContext directly.

### Option 2: Application-layer `ISalesPortalDbContext` exposing `IQueryable<T>`
- **Pros**: Decouples Application from `DbSet<T>`. Supports clean LINQ queries.
- **Cons**: Still leaks EF-specific LINQ translation behavior to application use cases.

### Option 3: Dedicated Read/Write Abstractions (CQRS)
- **Pros**: Infrastructure services query concrete `DbContext` directly for read models (optimizing projections). Write use cases depend on narrow aggregate repositories.
- **Cons**: High ceremony, requires maintaining repositories and DTO models.

### Option 4: Direct `DbContext` Dependency in Application
- **Pros**: Highly pragmatic. EF Core is treated as an application-level dependency. Maximizes access to native LINQ, split queries, and transaction APIs.
- **Cons**: Couples Application layer to Entity Framework Core concrete type.

---

## 2. Decision Proposal

We propose **Option 4 (Direct DbContext Dependency in Application)** as the most pragmatic approach.
- Reason: Mocking DbContext or `IQueryable` in unit tests is an anti-pattern that often leads to false positives. We will write integration tests against an actual in-memory SQLite database or a test SQL Server database to verify query translation.
- Disabling tracking will be explicitly managed in queries via `.AsNoTracking()`.
- Future portal boundaries will use separate folders or logical configurations within the same DbContext, keeping logical separations clean without the overhead of abstract context interfaces.
