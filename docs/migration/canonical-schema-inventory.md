# Canonical Schema Inventory

This document maps all 23 business entities from legacy MySQL source tables to the target Microsoft SQL Server database schemas.

---

## 1. Table Schema Translation Mapping

### 1. `users` Table
- **MySQL Source**:
  - Columns: `id` (bigint(20) unsigned, PK, auto_increment), `name` (varchar(255)), `email` (varchar(255), Unique), `avatar` (varchar(255), Nullable), `user_type` (varchar(255), Default 'Salesman'), `is_active` (tinyint(1), Default 1), `password` (varchar(255)), `remember_token` (varchar(100), Nullable), `email_verified_at` (timestamp, Nullable), `last_login_at` (timestamp, Nullable), `last_login_ip` (varchar(255), Nullable), `profit_target` (decimal(10,3)), `monthly_target` (decimal(10,3)), `profit_generated` (decimal(10,3)), `incentive` (decimal(10,3)), `basic_salary` (decimal(10,3)), `housing_allowance` (decimal(10,3)), `transport_allowance` (decimal(10,3))
- **SQL Server Target**:
  - Columns: `id` (bigint, PK, IDENTITY), `name` (nvarchar(255)), `email` (nvarchar(255), Unique), `avatar` (nvarchar(255), Nullable), `user_type` (nvarchar(255)), `is_active` (bit, Default 1), `password` (nvarchar(255)), `remember_token` (nvarchar(100), Nullable), `email_verified_at` (datetime2, Nullable), `last_login_at` (datetime2, Nullable), `last_login_ip` (nvarchar(255), Nullable), `profit_target`/`monthly_target`/`profit_generated`/`incentive`/`basic_salary`/`housing_allowance`/`transport_allowance` (decimal(15,3))
- **Transformation Rule**: Convert `tinyint(1)` to `bit`. Convert `timestamp` to `datetime2`.

### 2. `heads` Table
- **MySQL Source**:
  - Columns: `id` (bigint unsigned, PK), `created_by` (bigint unsigned, Nullable, FK to users.id), `user_id` (varchar(255), unique, Nullable), `type` (varchar(255), default 'customer'), `first_name` (varchar(255), Nullable), `last_name` (varchar(255), Nullable), `mobile_no` (varchar(20)), `email` (varchar(255), unique), `nationality` (varchar(255)), `balance` (decimal(10,3)), `balance_amount` (decimal(10,3)), `wallet` (decimal(10,3)), `address` (text), `status` (enum('active','inactive'), default 'active'), `deleted_at` (timestamp, Nullable)
- **SQL Server Target**:
  - Columns: `id` (bigint, PK, IDENTITY), `created_by` (bigint, Nullable, FK), `user_id` (nvarchar(255), unique, Nullable), `type` (nvarchar(255)), `first_name` (nvarchar(255), Nullable), `last_name` (nvarchar(255), Nullable), `mobile_no` (nvarchar(20)), `email` (nvarchar(255), unique), `nationality` (nvarchar(255)), `balance`/`balance_amount`/`wallet` (decimal(15,3)), `address` (nvarchar(max)), `status` (nvarchar(20), default 'active', CHECK constraint `status IN ('active','inactive')`), `deleted_at` (datetime2, Nullable)
- **Delete Behavior**: `ON DELETE CASCADE` (matches legacy migration constraint `heads_created_by_foreign`).

### 3. `head_ledgers` Table
- **MySQL Source**:
  - Columns: `id` (bigint unsigned, PK), `head_id` (bigint unsigned, FK), `service_type` (varchar(255)), `service_id` (bigint unsigned), `debit` (decimal(15,3)), `credit` (decimal(15,3)), `description` (varchar(255), Nullable)
- **SQL Server Target**:
  - Columns: `id` (bigint, PK, IDENTITY), `head_id` (bigint, FK), `service_type` (nvarchar(255)), `service_id` (bigint), `debit`/`credit` (decimal(15,3)), `description` (nvarchar(255), Nullable)
- **Delete Behavior**: `ON DELETE CASCADE` (matches legacy).

### 4. `receipts` Table
- **MySQL Source**:
  - Columns: `id` (bigint unsigned, PK), `created_by` (bigint unsigned, Nullable, FK), `receipt_id` (varchar(255), unique), `head_id` (bigint unsigned, Nullable, FK), `booking_id` (varchar(255), Nullable), `narration` (varchar(255)), `service` (varchar(255)), `amount` (decimal(10,3)), `payment_mode` (enum('cash','online','wallet')), `payment_proof` (varchar(255), Nullable), `status` (enum('pending','collection','collected','success'), default 'pending'), `payment_reference` (varchar(255), Nullable), `bank_id` (bigint, Nullable), `bank_name` (varchar(255), Nullable), `agent_id` (bigint, Nullable), `deleted_at` (timestamp, Nullable)
- **SQL Server Target**:
  - Columns: `id` (bigint, PK, IDENTITY), `created_by` (bigint, Nullable, FK), `receipt_id` (nvarchar(255), unique), `head_id` (bigint, Nullable, FK), `booking_id` (nvarchar(255), Nullable), `narration` (nvarchar(255)), `service` (nvarchar(255)), `amount` (decimal(15,3)), `payment_mode` (nvarchar(20)), `payment_proof` (nvarchar(255), Nullable), `status` (nvarchar(20)), `payment_reference` (nvarchar(255), Nullable), `bank_id` (bigint, Nullable), `bank_name` (nvarchar(255), Nullable), `agent_id` (bigint, Nullable), `deleted_at` (datetime2, Nullable)

### 5. `flight_bookings` Table
- **MySQL Source**:
  - Columns: `id` (bigint unsigned, PK), `user_id` (bigint unsigned, FK), `head_id` (bigint unsigned, Nullable, FK), `client_type` (varchar(255)), `agent` (varchar(255), Nullable), `agent_id` (bigint, Nullable), `care_of` (varchar(255), Nullable), `supplier` (varchar(255), Nullable), `from_location` (varchar(255)), `to_location` (varchar(255)), `payment_method` (varchar(255)), `payment_status` (varchar(255)), `processing_status` (varchar(255)), `payable_amount` (decimal(10,3)), `payment_proof` (varchar(255), Nullable)
- **SQL Server Target**:
  - Columns: `id` (bigint, PK, IDENTITY), `user_id` (bigint, FK), `head_id` (bigint, Nullable, FK), `client_type`/`agent`/`care_of`/`supplier`/`from_location`/`to_location`/`payment_method`/`payment_status`/`processing_status`/`payment_proof` (nvarchar(255)), `payable_amount` (decimal(15,3))

### 6-23. Other Service Booking and Govt Fee Tables
All other tables follow the identical pattern of:
- Bigint unsigned PKs mapped to `bigint` identity columns.
- Varchar columns mapped to `nvarchar` with identical lengths.
- Decimals mapped to `decimal(15,3)` to maintain consistency across the financial ledger.
- Time values mapped to `time` (C# `TimeSpan`), Dates to `date` (C# `DateTime`).
- Soft deletes mapped to nullable `deleted_at` `datetime2` columns.
- Booleans mapped to `bit`.
