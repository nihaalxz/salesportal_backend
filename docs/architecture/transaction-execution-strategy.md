# Transaction Execution Strategy

This document details the transaction management abstraction used to enforce provider-supported resilient execution blocks under connections failure scenarios.

---

## 1. Context & Architecture

ASP.NET Core applications using EF Core must wrap custom multi-statement transactions within the configured DB execution strategy to support transaction retries.
To prevent developers from accidentally starting database transactions outside retry handlers (e.g. `BeginTransactionAsync`), we introduce the `ITransactionalExecutor` abstraction.

- **Interface**: [ITransactionalExecutor.cs](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/src/TexasPortal.Application/Common/Interfaces/ITransactionalExecutor.cs)
- **Concrete Class**: [TransactionalExecutor.cs](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/src/TexasPortal.Infrastructure/Persistence/TransactionalExecutor.cs)

---

## 2. Abstraction Design

```csharp
public interface ITransactionalExecutor
{
    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);

    Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
}
```

### Rationale:
- **Transaction Scope**: The concrete executor wraps delegate operations within `strategy.ExecuteAsync()`, begins a database transaction, commits upon success, and executes rollback on failure.
- **Save Changes Ownership**: The delegate calls `SaveChangesAsync(cancellationToken)` explicitly before committing, allowing fine-grained execution boundaries without forcing it into the executor context.
- **Nested Transactions Policy**: SQL Server does not support nested database transactions. If an operation calls another transactional method, the executor execution checks for active transactions and joins them instead of creating nested boundaries.
