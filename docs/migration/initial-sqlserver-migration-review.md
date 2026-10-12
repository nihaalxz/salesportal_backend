# Initial SQL Server Migration Review

This document audits the generated migration [20260710131927_InitialSqlServerSchema.cs](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/src/TexasPortal.Infrastructure/Migrations/20260710131927_InitialSqlServerSchema.cs) against canonical schema specifications.

---

## 1. Column and Type Audits

### 1. Decimal Mappings Review
All standard BHD monetary fields are correctly defined with precision 15 and scale 3:
- **`users`**: `profit_target`, `monthly_target`, `profit_generated`, `incentive`, `basic_salary`, `housing_allowance`, `transport_allowance` mapped as `decimal(15,3)`.
- **`heads`**: `balance`, `balance_amount`, `wallet` mapped as `decimal(15,3)`.
- **`head_ledgers`**: `debit`, `credit` mapped as `decimal(15,3)`.
- **`receipts`**: `amount` mapped as `decimal(15,3)`.
- **Bookings (`flight_bookings`, `hotel_bookings`, `holiday_packages`, `vfs_appointments`, `transportations`, `other_services`)**: `payable_amount`, `expected_supplier_cost`, `profit` mapped as `decimal(15,3)`.

### 2. Primary Keys & Identity Behavior
- All PKs are declared as `long` (`bigint` in SQL Server).
- All PKs (excluding `notifications` which uses `nvarchar(36)`) use `.Annotation("SqlServer:Identity", "1, 1")`. This correctly replaces MySQL `AUTO_INCREMENT`.

### 3. Date and Time Fields
- Auditing timestamps (`created_at`, `updated_at`, `deleted_at`) mapped to `datetime2` (supports higher precision than standard `datetime`).
- Time-only values (e.g. `drop_time`, `appointment_time`) mapped to `time` (SQL Server native time span type).

---

## 2. Foreign Keys & Cascades Review

To prevent multi-cascade path cycle errors in SQL Server, the following constraints were successfully mitigated to `ReferentialAction.Restrict` (matching `DeleteBehavior.Restrict`):
- `FK_flight_bookings_heads_head_id`
- `FK_flight_bookings_users_user_id`
- `FK_holiday_packages_heads_head_id`
- `FK_holiday_packages_users_created_by`
- `FK_hotel_bookings_heads_head_id`
- `FK_hotel_bookings_users_user_id`
- `FK_other_services_heads_head_id`
- `FK_other_services_users_created_by`
- `FK_sales_visa_applications_heads_head_id`
- `FK_sales_visa_applications_users_user_id`
- `FK_transportations_heads_head_id`
- `FK_transportations_users_created_by`
- `FK_vfs_appointments_heads_head_id`
- `FK_vfs_appointments_users_created_by`

All other standard dependent details (e.g. passengers, applicants, documents) use `ReferentialAction.Cascade`.

---

## 3. Review Outcome

- **Approval Status**: **PASSED**.
- No unexpected schema operations or data loss risks were identified. Down migrations cleanly execute `DropTable` for all 23 tables.
