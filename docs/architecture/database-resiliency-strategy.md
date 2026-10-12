# Database Resiliency Strategy

This document details transient fault handling, execution retry policies, and transaction resiliency configuration in EF Core.

---

## 1. Global Resiliency Configuration

To ensure transient connection errors (e.g. database failovers, query time-outs) do not disrupt API requests, connection retries are enabled globally in the Infrastructure layer:

- **Location**: [DependencyInjection.cs](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/src/TexasPortal.Infrastructure/DependencyInjection.cs)
- **Configuration**:
  ```csharp
  services.AddDbContext<SalesPortalDbContext>(options =>
  {
      options.UseSqlServer(connectionString, sqlServerOptions =>
      {
          sqlServerOptions.EnableRetryOnFailure(
              maxRetryCount: 5,
              maxRetryDelay: TimeSpan.FromSeconds(30),
              errorNumbersToAdd: null);
      });
  });
  ```

---

## 2. Transactions Resiliency Pattern

Since `EnableRetryOnFailure` is configured globally, any explicit user-initiated transaction must be wrapped inside the execution strategy block to ensure correct rollback and retry behavior.

### Recommended Transaction Pattern
For all booking creations and ledger postings in future write phases, the following resilient pattern must be enforced:

```csharp
var strategy = dbContext.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () =>
{
    using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
    try
    {
        // Perform database operations
        // ...
        
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
    catch (Exception)
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
});
```
This pattern ensures the entire transaction is executed and retried as a single atomic unit under connection failures.
