# MySQL Source Schema Profile

> [!WARNING]
> **Status**: **BLOCKED — SAFE DATABASE CONNECTION NOT CONFIGURED OR OFFLINE**.
> Local port `3307` is closed. Local port `3306` is open but requires root credentials not provided or configured in the environment.
> Therefore, runtime metadata profiling (from `sys.tables` or actual rows count) could not be executed against a live database instance.
>
> **Fallback Evidence Used**:
> - [schema.sql](file:///d:/texas_sales_portal/schema.sql) (snapshot database DDL).
> - 91 Laravel migration files inside `database/migrations/`.

---

## 1. Migration History and Key Changes

Through static analysis of the migration log files, the following evolutionary changes are identified in the legacy schema:
- **Service Decimal Standardization** (`2026_02_19_084727_standardize_decimals_to_3_places.php`):
  Standardized monetary values to `decimal(10,3)` for precise BHD (Bahraini Dinar) storage (3 decimal places).
- **Composite Unique Constraints** (`2026_03_29_144206_add_unique_constraint_to_head_ledgers_table.php`):
  Enforces a unique index on `head_ledgers` for `(service_type, service_id)` composite values, preventing double ledger postings.
- **Head Balance recalculations**:
  `heads` table balance columns evolved from a single `balance` field to include `balance_amount` and `wallet`.

## 2. Table Schemas from Migrations

All 23 mapped tables have been mapped from their respective schema definitions:
- **`users`**: PK `id` bigint, string `email` unique, `is_active` tinyint.
- **`heads`**: PK `id` bigint, string `user_id` unique, `email` unique, `balance` decimal(10,3), `wallet` decimal(10,3).
- **`head_ledgers`**: PK `id` bigint, `head_id` bigint, `service_type` varchar, `service_id` bigint, `debit`/`credit` decimal(15,3).
- **`receipts`**: PK `id` bigint, `receipt_id` varchar unique, `amount` decimal(10,3), `status` enum.
