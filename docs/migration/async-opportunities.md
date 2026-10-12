# Asynchronous Opportunities Analysis

This document identifies major I/O-bound operations and maps them to async/await patterns, cancellation strategies, and safety assessments.

---

## 1. Asynchronous I/O Mapping Table

| Operation | Laravel Source | I/O Type | Current Behavior | .NET Async API | Cancellation Strategy | Parallel Safe? | Expected Benefit |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Get Dashboard Stats** | `DashboardController` | DB Read | Issues 15 sequential sync queries | EF Core async APIs | Propagate `CancellationToken` | **No** (if using same DbContext) | Reduces thread-blocking; allows faster frontend loading |
| **Create Booking** | `FlightBookingService` | DB Write | Synchronous insert & save | `AddAsync`, `SaveChangesAsync` | Propagate `CancellationToken` | **No** (Requires sequential inserts for foreign keys) | Improves API throughput and release of connections |
| **Verify Receipt & Allocate** | `BookingPaymentService` | DB + HTTP | Synchronous DB updates + sync external HTTP requests | `SaveChangesAsync`, `HttpClient.PostAsync` | Propagate `CancellationToken` | **No** (Requires strict transaction serializability for financial safety) | Eliminates transaction holding times; avoids thread starvation |
| **Sync Remote Staff / Cost** | `StaffSyncController` | DB Write | Synchronous database lookup and update | `FirstOrDefaultAsync`, `SaveChangesAsync` | Propagate `CancellationToken` | **Yes** (different salesman records) | Prevents Operations portal timeouts during bulk syncs |
| **File Proof Upload** | `ReceiptController` | Disk Write | Synchronous file save to local storage | `FileStream.WriteAsync` | Propagate `CancellationToken` | **Yes** | Releases web threads during slow network file streams |

---

## 2. Parallelization Rules & Safe Practices

> [!WARNING]
> - **EF Core DbContext Safety**: A single `DbContext` instance is **not thread-safe**. Running concurrent queries (e.g. executing `Task.WhenAll` on queries using the same context) will throw a `DbConcurrencyException` or corrupt connection state.
> - **Financial Transactions**: Parallelizing ledger entries or balance updates is strictly prohibited due to race condition risks on Wallet and Head balance calculations.
> - **Safe Parallel Aggregations**: If dashboard metrics must be loaded in parallel, request-level parallelization must be done either at the frontend/API gateway layer (by hitting independent endpoints) or by using a `IDbContextFactory<TexasPortalDbContext>` to spawn separate context instances for read queries.
