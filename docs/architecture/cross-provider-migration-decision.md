# Cross-Provider Database Migration Decision

**Status**: Accepted (SQL Server is selected as the target database provider)

---

## 1. Migration Options

### Option A: MySQL-to-MySQL Modernization (Staged Migration)
* **Strategy**: The new C# backend runs on MySQL in the first stage. Business logic is migrated, tested, and deployed to production. Once verified, a second separate project is launched to migrate the MySQL database to SQL Server.
* **Advantages**:
  - Excludes data translation risk from the primary code modernization phase.
  - Zero schema mapping translation risks (types, collations, nullability remain 100% identical).
  - Simpler coexistence and rollback (the Laravel portal and C# backend can query the exact same MySQL tables simultaneously).
* **Disadvantages**:
  - Postpones the target SQL Server database deployment, extending legacy database licensing or operations overhead.
  - Requires double-testing data access (first on MySQL, then later on SQL Server).

### Option B: MySQL-to-SQL-Server Modernization (Parallel Switch)
* **Strategy**: Migrate both the backend framework (PHP to .NET) and the database engine (MySQL to SQL Server) simultaneously. A new SQL Server database is created, legacy MySQL data is transformed and loaded, and the C# API goes live on SQL Server.
* **Advantages**:
  - Single-stage modernization: no need for a secondary database migration project.
  - Instantly utilizes SQL Server benefits (e.g. native `bit` types, `datetime2`, optimized column indexing, schema-level enforcement).
* **Disadvantages**:
  - High risk of data type conversion issues, collation mismatches, and precision differences.
  - Requires a comprehensive data migration (ETL) pipeline, strict validation, and an explicit rollback path.

---

## 2. Risk & Complexity Matrix

| Parameter | Option A (Staged) | Option B (Parallel Switch) |
| :--- | :--- | :--- |
| **Operational Complexity** | Low (same DB, dual connection) | High (requires ETL pipeline & schema synchronization) |
| **Data Migration Risk** | Zero (no data moved initially) | High (requires translation of nulls, datetimes, decimals) |
| **Financial Correctness** | High Guarantee | Risk of precision/rounding drift (Requires strict checks) |
| **Downtime Requirements** | Low (incremental cutover) | Medium to High (requires read-only mode during ETL) |
| **Rollback Complexity** | Low (Laravel is active on same DB) | High (requires reverse data sync from SQL Server back to MySQL) |
| **Coexistence Support** | High (both run on MySQL) | Low (requires dual-writing or isolated read-only syncs) |

---

## 3. Decision

**Option B (Parallel Switch)** is selected, constrained by the target database host requirement. 
To mitigate the risks of Option B, we establish:
1. An explicit **Canonical Schema Inventory** to track type conversions.
2. A **Financial Reconciliation strategy** verifying ledger balances, wallets, and cash down to 3 decimal places before cutover.
3. A dry-run rehearsal policy on staging prior to production cutover.
