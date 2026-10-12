# Data Migration Validation Plan

This document outlines the validation test plan to execute on the SQL Server staging database after loading the transformed MySQL data.

---

## 1. Post-Migration Test Suite

We will execute automated validation tests verifying the following parameters:

### Schema Fidelity & Integrity
- **Row Count Matching**: Count records in each table in MySQL vs SQL Server. They must match exactly.
- **Identity Seed Integrity**: Verify SQL Server identity columns seed values match the maximum ID loaded from MySQL to prevent future insert failures:
  ```sql
  SELECT IDENT_CURRENT('users') as current_seed;
  ```

### Structural Mappings Validation
- **Primary and Foreign Key Integrity**: Identify any orphan rows where constraint checks fail. Ensure no foreign keys point to missing parent IDs.
- **NULL Preservation**: Check that nullable columns that are empty in MySQL are stored as `NULL` in SQL Server, rather than blank strings.
- **String Length Truncation**: Assert that string column lengths in the migrated dataset do not exceed maximum character constraints.

### Financial Checking
- **Financial Reconciliation Checks**: Execute the calculations defined in the [Financial Reconciliation Strategy](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/docs/architecture/financial-reconciliation-strategy.md).
- **Decimal Precision Exactness**: Verify that decimal places of BHD currencies are preserved (no trailing zeros dropped during conversion).

---

## 2. Test Execution Guidelines

1. **Dry-Run Auditing**:
   - The validation plan must be executed on a staging database populated with a snapshot copy of the live Laravel database.
2. **Safe Test Executions**:
   - Do NOT run tests that perform database modifications or schema changes on shared databases. All testing is strictly read-only validation.
