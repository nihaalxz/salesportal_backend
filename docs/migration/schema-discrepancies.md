# Schema Discrepancies

This document records the design reconciliations and data-type changes between the legacy Laravel MySQL schema and the new C# Microsoft SQL Server target schema.

---

## 1. Mapped Discrepancies

| ID | Table | Column/Relationship | MySQL Behavior | Chosen .NET Mapping | Reason & Resolution | Risk |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **DISC-001** | `users` | `profit_target` etc. | `decimal(10,3)` (Max 9,999,999.999 BHD) | `decimal(15,3)` (Max 999,999,999,999.999 BHD) | Expanded range to prevent overflow under future high sales volume while maintaining scale. | None. |
| **DISC-002** | `heads` | `status` | `enum('active', 'inactive')` | `nvarchar(20)` | SQL Server doesn't natively support enum types without database-specific structures. Using string property maps cleanly. | Low (validation handled in Application layer). |
| **DISC-003** | `receipts` | `payment_mode` | `enum('cash', 'online', 'wallet')` | `nvarchar(20)` | Using string property to ensure backward compatibility and avoid hard migrations. | Low. |
| **DISC-004** | All tables | `created_at`, `updated_at`, `deleted_at` | `timestamp` / `datetime` | `datetime2` | Standard C# DateTime maps directly to SQL Server `datetime2`. Offset queries are normalized. | Low. |
| **DISC-005** | `cash_in_hands` | `is_active`/`is_inhouse` | `tinyint(1)` | `bit` | Boolean fields mapped to bit type in SQL Server. | None. |
