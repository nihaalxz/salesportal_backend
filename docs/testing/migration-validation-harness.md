# Migration Validation Harness Foundation

This document details the architecture and features of the automated database validation harness implemented to verify schema correctness.

---

## 1. Objectives & Approach

The validation harness provides automated assertions that verify that the deployed target schema matches the domain specifications before any business read/write operations execute.
- **Source Code**: [MigrationValidationTests.cs](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/tests/TexasPortal.IntegrationTests/MigrationValidationTests.cs)
- **Trait Category**: `MigrationValidation`

---

## 2. Validation Scopes

The harness currently checks:
1. **Table Coverage**: Ensures all 23 expected tables exist in the target schema database.
2. **Column Attributes**: Verifies decimal data types have exactly `scale = 3` and `precision = 15`.
3. **Shadow Column Audit**: Scans columns ending in `Id1` or other auto-generated EF Core relationship properties to ensure clean mapping design without duplicate fields.

---

## 3. Extending the Harness

For future phases, the validator can be extended to support:
- **Checksum Verification**: Validating binary column parity.
- **FK Constraints Audit**: Querying `sys.foreign_keys` to ensure correct cascading behaviors.
- **Reconciliation Counts**: Verifying row count parity between legacy database and migrated database.
