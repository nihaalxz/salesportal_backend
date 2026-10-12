# Sales Read Query Tuning & Indexing Guidelines

To prevent performance degradation as the database grows, specific indexes must be applied to target SQL Server database tables. All read operations in Phase 8 use ownership filters and soft delete markers (`DeletedAt`).

---

## 1. Mapped Columns Requiring Indexes

The following columns are heavily queried in `WHERE` clauses and should have non-clustered indexes.

### Core Ownership & Status Indexes

```sql
-- Heads: ownership, status and soft deletes
CREATE NONCLUSTERED INDEX IX_Heads_CreatedBy_DeletedAt
ON Heads (CreatedBy, DeletedAt)
INCLUDE (FirstName, LastName, Email, Balance, Wallet);

-- Leads: ownership, status and soft deletes
CREATE NONCLUSTERED INDEX IX_Leads_CreatedBy_DeletedAt
ON Leads (CreatedBy, DeletedAt)
INCLUDE (Name, Status, MobileNo);

-- Bookings: user ownership (all service types)
CREATE NONCLUSTERED INDEX IX_FlightBookings_UserId
ON FlightBookings (UserId)
INCLUDE (FromLocation, ToLocation, PayableAmount);

CREATE NONCLUSTERED INDEX IX_HotelBookings_UserId
ON HotelBookings (UserId)
INCLUDE (GuestName, CheckInDate, PayableAmount);

CREATE NONCLUSTERED INDEX IX_SalesVisaApplications_UserId
ON SalesVisaApplications (UserId)
INCLUDE (VisaPackName, PayableAmount);

CREATE NONCLUSTERED INDEX IX_VfsAppointments_CreatedBy_DeletedAt
ON VfsAppointments (CreatedBy, DeletedAt)
INCLUDE (Name, AppointmentDate, PayableAmount);
```

---

## 2. EF Core Query Projections Analysis

Every read endpoint uses `.Select(x => new DTO { ... })` and `.AsNoTracking()`.
- **Memory Efficiency**: Avoids EF Core change tracker overhead completely.
- **Network Bandwidth**: Selects only the needed columns (e.g. avoids downloading large blobs like `PassportCopy` or `PaymentProof` on list view).
- **SQL Execution Plan**: Allows SQL Server to perform Index-Only scans where the query can be fully resolved from non-clustered indexes (via `INCLUDE` columns).
