# Sales Read Generated SQL Review

This document audits representative SQL queries compiled by EF Core for Sales Read endpoints.

---

## 1. Customer Heads List Query

```sql
-- Generated SQL for heads listing with creator scoping
SELECT [h].[Id], [h].[FirstName], [h].[LastName], [h].[Email], [h].[MobileNo], [h].[Nationality], [h].[Type], [h].[Status], [h].[Balance], [h].[Wallet], [h].[CreatedAt]
FROM [Heads] AS [h]
WHERE [h].[CreatedBy] = @__userId_0 AND [h].[DeletedAt] IS NULL
ORDER BY [h].[LastName]
OFFSET @__p_1 ROWS FETCH NEXT @__p_2 ROWS ONLY;
```

* **Selectivity**: High. Uses the primary index key (`CreatedBy`, `DeletedAt`).
* **Columns**: Sourced columns match the `HeadSummaryResponse` DTO exactly. No over-fetching of large fields (like `Address`).

---

## 2. Flight Bookings Query

```sql
SELECT [f].[Id], [f].[FromLocation], [f].[ToLocation], [f].[PaymentMethod], [f].[PaymentStatus], [f].[ProcessingStatus], [f].[PayableAmount], [f].[CreatedAt],
       (SELECT COUNT(*) FROM [FlightPassengers] AS [p] WHERE [f].[Id] = [p].[FlightBookingId]) AS [PassengerCount]
FROM [FlightBookings] AS [f]
WHERE [f].[UserId] = @__userId_0
ORDER BY [f].[CreatedAt] DESC
OFFSET @__p_1 ROWS FETCH NEXT @__p_2 ROWS ONLY;
```

* **Aggregation**: Passenger count aggregation is performed directly inside the SQL query, avoiding loading list graphs into memory.
