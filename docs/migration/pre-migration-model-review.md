# Pre-Migration Model Review

This document audits the mapped EF Core DbContext configurations before generating database migrations, verifying schema alignment against target SQL Server constraints.

---

## 1. Mapped Entity Audits

Every entity was programmatically reviewed using model metadata checks. The results are summarized below:

| Entity / Table Name | Primary Key | Identity Behavior | Decimal Properties (Scale/Precision) | SQL Server Reserved Keywords | Soft Delete Column | Multiple Cascade Paths |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`users`** | `id` (bigint) | `IDENTITY` | `BasicSalary`, `HousingAllowance` (15, 3) | `user_type` | None | None |
| **`heads`** | `id` (bigint) | `IDENTITY` | `Balance`, `Wallet` (15, 3) | `type`, `status` | `deleted_at` | Yes (Cascades resolved to Restrict in VFS / Holiday tables) |
| **`head_ledgers`** | `id` (bigint) | `IDENTITY` | `Debit`, `Credit` (15, 3) | None | None | None |
| **`receipts`** | `id` (bigint) | `IDENTITY` | `Amount` (15, 3) | `status` | `deleted_at` | None |
| **`flight_bookings`** | `id` (bigint) | `IDENTITY` | `PayableAmount` (15, 3) | None | None | None |
| **`flight_passengers`** | `id` (bigint) | `IDENTITY` | `Amount`, `SupplierCost`, `Profit` (15, 3) | None | None | None |
| **`hotel_bookings`** | `id` (bigint) | `IDENTITY` | `Amount`, `SupplierCost`, `Profit` (15, 3) | None | None | None |
| **`sales_visa_applications`** | `id` (bigint) | `IDENTITY` | `RejectionFee` (15,3) | None | None | None |
| **`sales_visa_applicants`** | `id` (bigint) | `IDENTITY` | `Amount`, `SupplierCost`, `Profit` (15, 3) | None | None | None |
| **`vfs_appointments`** | `id` (bigint) | `IDENTITY` | `Amount`, `SupplierCost`, `Profit` (15, 3) | None | `deleted_at` | Yes (Configured as `DeleteBehavior.Restrict`) |
| **`holiday_packages`** | `id` (bigint) | `IDENTITY` | `Amount`, `SupplierCost`, `Profit` (15, 3) | None | `deleted_at` | Yes (Configured as `DeleteBehavior.Restrict`) |
| **`transportations`** | `id` (bigint) | `IDENTITY` | `Amount`, `SupplierCost`, `Profit` (15, 3) | None | `deleted_at` | Yes (Configured as `DeleteBehavior.Restrict`) |
| **`other_services`** | `id` (bigint) | `IDENTITY` | `Amount`, `SupplierCost`, `Profit` (15, 3) | None | `deleted_at` | Yes (Configured as `DeleteBehavior.Restrict`) |
| **`cash_in_hands`** | `id` (bigint) | `IDENTITY` | `Amount` (15, 3) | None | None | None |
| **`deposits`** | `id` (bigint) | `IDENTITY` | `Amount` (15, 3) | `status`, `type` | None | None |
| **`staff_monthly_records`** | `id` (bigint) | `IDENTITY` | `ProfitGenerated`, `BasicSalary` (15, 3) | None | None | None |
| **`leads`** | `id` (bigint) | `IDENTITY` | None | `status` | None | None |
| **`disputes`** | `id` (bigint) | `IDENTITY` | `RefundAmount` (15, 3) | `status` | None | None |

---

## 2. Special Audit Finding & Safety Mitigations

### 1. SQL Server Reserved Word Safety
- The column names `status`, `type`, and `user_type` are SQL Server keywords.
- **Mitigation**: EF Core automatically generates bracket-escaped SQL queries (e.g. `[status]`, `[type]`) when compiling commands. No application-level column renamings are necessary.

### 2. Cascade Path Warn Resolving
- If both `created_by` (users) and `head_id` (heads) cascade delete to service bookings, and `heads` cascades from `users`, SQL Server detects a potential infinite delete loop.
- **Mitigation**: All service booking constraints targeting `users` and `heads` have been explicitly set to `DeleteBehavior.Restrict` in `EntityConfigurations.cs`.

### 3. Shadow Properties and Mappings Validation
- Tests verify there are no unexpected shadow foreign keys (e.g. `UserId1`) which usually indicate misconfigured navigation properties.
