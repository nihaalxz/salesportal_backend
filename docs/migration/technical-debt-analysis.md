# Technical Debt Analysis

This document identifies major weaknesses, architectural bottlenecks, and financial safety risks in the existing Laravel Sales Portal codebase, and outlines recommended improvements for the ASP.NET Core backend.

---

### DEBT-IO-001: Synchronous External HTTP Calls Inside Database Transactions

- **Category**: Integration & Performance
- **Laravel Source**: `app/Services/BookingPaymentService.php` (lines 87-99)
- **Current Behavior**:
  Within the `DB::transaction` block in `allocateReceipt()`, the service executes synchronous HTTP calls to `AccountsApiService::updateAgentBalance` and `updateAgentWallet`.
- **Problem**:
  Holding a database transaction open while performing external HTTP calls block database connections, increasing lock contention, and leading to pool starvation.
- **Operational Impact**: High latency spikes under load; db connection exhaustion.
- **Performance Impact**: High transaction holding times.
- **Security Impact**: None.
- **Correctness Risk**: High. The code catches `Exception` and suppresses it (`Log::error`). If the external Accounts portal fails to update the agent balance, the local transaction *still commits successfully*, leading to critical ledger discrepancies between the two systems.
- **Maintainability Impact**: Hard to debug synchronization failures due to lack of retry mechanics.
- **Recommended .NET Improvement**:
  Perform external HTTP operations *outside* the local database transaction boundary. To guarantee eventual consistency without blocking, implement a Transactional Outbox pattern or queue remote synchronization tasks via hosted background tasks using `Channel<T>` for in-process execution, complete with retries.
- **Priority**: High
- **Migration Phase**: Phase 12 & 13

---

### DEBT-CONC-002: Read-Modify-Write Concurrency Risk on Head Balances

- **Category**: Concurrency Safety & Financial Correctness
- **Laravel Source**: `app/Services/HeadLedgerService.php` (lines 48-50)
- **Current Behavior**:
  Updates the Head's ledger balance using:
  ```php
  $head->balance_amount += $diff;
  $head->balance += $diff;
  $head->save();
  ```
- **Problem**:
  Standard Eloquent model updates load the model state into memory, perform arithmetic, and issue an `UPDATE` statement. If two requests execute concurrently for the same Head, they will both read the same initial balance, perform calculations, and write back stale values, overwriting each other.
- **Operational Impact**: Inaccurate customer and agent balances, resulting in financial loss or incorrect credit limits.
- **Correctness Risk**: High.
- **Recommended .NET Improvement**:
  Use EF Core optimistic concurrency tokens (e.g. `[ConcurrencyCheck]` or row version tokens) to detect conflict, or execute atomic database-level operations:
  ```csharp
  await db.Heads
      .Where(h => h.Id == headId)
      .ExecuteUpdateAsync(s => s.SetProperty(h => h.Balance, h => h.Balance + diff));
  ```
- **Priority**: High
- **Migration Phase**: Phase 12

---

### DEBT-DB-003: Multiple Sequential Database Round-Trips in Dashboard Metrics

- **Category**: Database Query Performance
- **Laravel Source**: `app/Http/Controllers/DashboardController.php` (lines 71-108)
- **Current Behavior**:
  The dashboard statistics are computed via 15 separate database query operations (count, sum, raw aggregates), including separate queries to aggregate sales for each booking type sequentially.
- **Problem**:
  Multiple round trips to the database server are executed sequentially, increasing dashboard loading times and memory allocations on the web server.
- **Performance Impact**: High latency on initial dashboard load.
- **Recommended .NET Improvement**:
  Build dedicated, optimized query handlers using EF Core projections. Combine counts and sums using database-side conditional aggregations (e.g. `GroupBy` or direct LINQ projection into DTOs) to minimize round trips and utilize database indexes efficiently. Use `AsNoTracking()` for all read-only queries.
- **Priority**: Medium
- **Migration Phase**: Phase 8
