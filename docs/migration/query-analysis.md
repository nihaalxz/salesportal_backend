# Database Query Analysis

This document evaluates database access patterns in the Laravel system and sets explicit standards for EF Core queries in the .NET backend.

---

## 1. Laravel Query Anti-Patterns Identified

### Eager Loading Overhead (`with(...)`)
- **Laravel Source**: `FlightBookingController::index` loads all flight passenger records and head details (`with('passengers', 'head')`) even if the view only displays top-level lists.
- **Problem**: Over-fetching columns and nested objects increases memory allocations and payload size.
- **.NET Strategy**: Project directly to custom DTO structures using `.Select()` to fetch only required fields, avoiding massive database joins and object-graph reconstruction.

### Sequential Loops with DB Saves
- **Laravel Source**: `BookingPaymentService::applyAllocation` retrieves lists of outstanding bookings and executes `$booking->save()` in a loop.
- **Problem**: Triggers multiple sequential network round-trips to update each row.
- **.NET Strategy**: Utilize EF Core batch updates `ExecuteUpdateAsync` or perform bulk writes inside a consolidated unit of work where possible.

---

## 2. EF Core Access Rules & Standards

To ensure optimal performance, the C# implementation must adhere to the following rules:

1. **Explicit No-Tracking**:
   Always append `.AsNoTracking()` to all read-only queries (e.g. dashboard lists, head search, booking lookup detail views). This disables tracking overhead in EF Core.
2. **Database-Side Execution**:
   - Ensure all filters, sorts, and pagination are translated to SQL. Never run `ToListAsync()` followed by in-memory `.Where()` filtering.
   - Use pagination (`.Skip(x).Take(y)`) on all list endpoints.
3. **No Lazy Loading**:
   Do not enable lazy-loading proxies. Eager loading must be done explicitly using `.Include()` only when actual domain mutations are required.
4. **Inspecting SQL**:
   Configure SQL Logging in Development (`appsettings.Development.json`) to output generated queries to the logger. Validate index usage for critical search operations.
