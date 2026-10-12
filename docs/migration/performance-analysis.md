# Performance Baseline & Analysis

This document identifies performance issues in the Laravel Sales Portal and outlines modernization improvements for the ASP.NET Core backend.

---

## 1. Key Performance Bottlenecks

### Dashboard Metrics Calculation
- **Laravel Implementation**: `DashboardController::getStatistics` issues 15 separate database calls to compute counts and sums across flight bookings, hotel bookings, transportation, visa applications, etc.
- **Query Duplication**: Calculates total sales by joining and calling `sum()` separately on 7 different booking tables.
- **Improvement**: Compile aggregates using consolidated LINQ projections. If necessary, execute queries in parallel using independent DbContext instances or combine the queries into a single database procedure/query containing conditional SQL `CASE` blocks.

### Payment Allocation Loops
- **Laravel Implementation**: `BookingPaymentService::applyAllocation` loads lists of outstanding bookings into memory (`$query->oldest()->get()`) and loops over them, calling `processSingleAllocation()` which performs individual model saves (`$booking->save()`) in a loop.
- **Problem**: This triggers multiple round-trip `UPDATE` commands sequentially.
- **Improvement**: Group outstanding bookings and process allocations. If multiple bookings need updating, batch the `UPDATE` statements or use EF Core's `ExecuteUpdateAsync` to update payment status efficiently in a single operation where possible, minimizing database traffic.

---

## 2. Recommended Database Indexes

Based on query behavior, the following indexes are critical and must be defined in the .NET schema:

1. **`head_ledgers`**:
   - Composite Index: `(head_id, service_type, service_id)` -> Used for checking already-allocated receipts and ledger audits.
2. **`receipts`**:
   - Index: `(head_id, status)` -> Used for listing verified customer payments.
3. **`bookings` tables** (Flight, Hotel, Visa, Vfs, Holiday, Transportation, Other):
   - Composite Index: `(head_id, payment_status)` -> Used heavily by the allocation engine to look up outstanding invoices.
   - Composite Index: `(user_id, created_at)` -> Used for displaying dashboard and paginated service list endpoints.
