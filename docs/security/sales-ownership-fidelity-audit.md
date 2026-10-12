# Sales Ownership Fidelity Audit

This audit validates that our generic ownership interfaces mapping to SQL Server match the business behavior of the legacy Laravel Sales Portal.

---

## 1. Entity Fields Analysis

| Entity | Laravel Owner | C# Ownership Field | Interface Mapping | Match? |
| :--- | :--- | :--- | :--- | :--- |
| **Head** | `created_by` | `CreatedBy` (long?) | `ICreatorOwned` | **Yes** |
| **Lead** | `created_by` | `CreatedBy` (long) | `ICreatorOwned` | **Yes** |
| **FlightBooking** | `user_id` | `UserId` (long) | `IUserOwned` | **Yes** |
| **HotelBooking** | `user_id` | `UserId` (long) | `IUserOwned` | **Yes** |
| **SalesVisaApplication** | `user_id` | `UserId` (long) | `IUserOwned` | **Yes** |
| **VfsAppointment** | `created_by` | `CreatedBy` (long) | `ICreatorOwned` | **Yes** |
| **HolidayPackage** | `created_by` | `CreatedBy` (long) | `ICreatorOwned` | **Yes** |
| **Transportation** | `created_by` | `CreatedBy` (long) | `ICreatorOwned` | **Yes** |
| **OtherService** | `created_by` | `CreatedBy` (long) | `ICreatorOwned` | **Yes** |
| **CashInHand** | `user_id` | `UserId` (long) | `IUserOwned` | **Yes** |
| **Deposit** | `user_id` | `UserId` (long) | `IUserOwned` | **Yes** |
| **StaffMonthlyRecord** | `user_id` | `UserId` (long) | `IUserOwned` | **Yes** |

---

## 2. Business Scope & Rules

1. **Heads**: Heads are owned by the salesman who created them (`CreatedBy = auth_id`). However, manager/admin portals can query Heads across all salesmen. The interface `ICreatorOwned` cleanly captures this creator context.
2. **Ledgers**: Ledger records (`HeadLedger`) do not have an independent user/creator column; instead, they are scoped through the parent `Head`'s ownership context. This prevents any access leakage.
3. **Deposits**: Sourced by the individual salesman account (`UserId`).
