# Flight Query Performance Review

This document audits the SQL execution paths, index usage, and query structures for the Flight Booking module to prevent N+1 query problems and ensure sub-second response times.

---

## 1. Index Audit & Verification

We reviewed the database schema file `schema_deploy.sql` to verify that appropriate indexes exist for columns frequently queried in filters, joins, and lookup operations.

### Key Index Mappings:
1. **Idempotency Key Check**:
   - Query: Lookup on `idempotency_key`
   - Index: `IX_IdempotencyRecords_IdempotencyKey` on `idempotency_records(idempotency_key)`
   - Status: **VERIFIED** (Unique index, ensures $O(1)$ lookup performance).
2. **Head Reference Integrity**:
   - Query: Foreign key join/lookup on `head_id`
   - Index: `IX_FlightBookings_HeadId` on `flight_bookings(head_id)`
   - Status: **VERIFIED**.
3. **Salesman Filter**:
   - Query: Retrieve bookings created by a specific salesman (`user_id` / `UserId`)
   - Index: `IX_FlightBookings_UserId` on `flight_bookings(user_id)`
   - Status: **VERIFIED**.
4. **Passenger List Retrieval**:
   - Query: Join passengers for a flight booking (`flight_booking_id` / `FlightBookingId`)
   - Index: `IX_FlightPassengers_FlightBookingId` on `flight_passengers(flight_booking_id)`
   - Status: **VERIFIED**.

---

## 2. N+1 Query Prevention

The query for the flights list endpoint in `BookingsController.cs` is implemented to retrieve bookings with their passengers in a single database roundtrip.

### Query Implementation:
The read service uses `Include` or projection to fetch the related passenger collections in one query:
```csharp
var bookings = await _db.FlightBookingsDbSet
    .Include(b => b.Passengers)
    .Where(b => b.UserId == userId)
    .OrderByDescending(b => b.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(ct);
```
EF Core translates this to an inner join or a split query (with `.AsSplitQuery()`) which executes exactly one or two queries respectively, regardless of the number of bookings retrieved ($O(1)$ SQL execution cost relative to record count).

---

## 3. Atomic EF Core Updates
Instead of loading entities, modifying properties, and saving changes (which requires two database roundtrips: Select + Update), balance mutations are executed directly via `ExecuteUpdateAsync`:
```sql
UPDATE [Heads] SET [Wallet] = [Wallet] - @amount WHERE [Id] = @headId;
```
This reduces network roundtrips and optimizes database lock durations, improving throughput under load.
