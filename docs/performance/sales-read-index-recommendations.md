# Sales Read Index Recommendations

This document outlines target index definitions to support Phase 8 read queries.

---

## 1. Required Non-Clustered Indexes

### Heads Queries Index
- **Index**: `IX_Heads_CreatedBy_DeletedAt`
- **Predicate**: `WHERE CreatedBy = @userId AND DeletedAt IS NULL`
- **Included Columns**: `FirstName`, `LastName`, `Email`, `Balance`, `Wallet`.

### Leads Queries Index
- **Index**: `IX_Leads_CreatedBy_DeletedAt`
- **Predicate**: `WHERE CreatedBy = @userId AND DeletedAt IS NULL`
- **Included Columns**: `Name`, `Status`, `MobileNo`.

### Booking Scopes Index
- **Index**: `IX_FlightBookings_UserId`
- **Predicate**: `WHERE UserId = @userId`
- **Included Columns**: `PayableAmount`, `CreatedAt`.
