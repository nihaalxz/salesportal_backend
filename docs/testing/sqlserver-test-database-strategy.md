# SQL Server Database Strategy

This document classifies and controls the databases used in the development, testing, and production phases of the Texas Sales Portal.

---

## 1. Database Classifications

### A. Production Database
- **Server Classification**: Managed cloud-hosted SQL Server
- **Database Classification**: Persistent Production environment
- **Database Name**: Configured via secure environment variables (`ConnectionStrings:DefaultConnection`) in production host settings.
- **Environment**: Production
- **Shared / Isolated**: Shared across production portal clients
- **Disposable / Persistent**: Strict Persistent database (never disposed or truncated)
- **Resettable**: NO. Destructive reset is blocked.
- **Contains Production Data**: Yes (authoritative live customer records)
- **Contains Deterministic Test Data**: No
- **Safe for Test Writes**: NO
- **Safe for Destructive Reset**: NO

### B. Shared Development Database
- **Server Classification**: Public shared database server (`db59312.public.databaseasp.net`)
- **Database Classification**: Persistent Development environment
- **Database Name**: `db59312`
- **Environment**: Development
- **Shared / Isolated**: Shared among development team members
- **Disposable / Persistent**: Persistent (contains manually seeded test cases for developers)
- **Resettable**: No (destructive operations or schema drops are manually managed and guarded)
- **Contains Production Data**: No (only fake staging records)
- **Contains Deterministic Test Data**: Partial (manually seeded records; not guaranteed to remain clean)
- **Safe for Test Writes**: Yes (during manual verification)
- **Safe for Destructive Reset**: NO

### C. Local Integration Test Database
- **Server Classification**: Local instance (`localhost\SQLEXPRESS` or `(localdb)\MSSQLLocalDB`)
- **Database Classification**: Disposable Test environment
- **Database Names**:
  - `TexasPortal_HTTP_Tests` (used by backend integration tests)
  - `TexasPortal_DevAuth_Tests` (used by dev-auth HTTP tests)
- **Environment**: Testing
- **Shared / Isolated**: Isolated (local developer machine only)
- **Disposable / Persistent**: Disposable (recreated on every test suite launch)
- **Resettable**: YES. Code runs `EnsureDeletedAsync()` to reset state deterministically.
- **Contains Production Data**: No
- **Contains Deterministic Test Data**: Yes (automatically seeded by test initialization)
- **Safe for Test Writes**: YES
- **Safe for Destructive Reset**: YES

### D. Local Development & Verification Database (Phase 10B Real Verification)
- **Server Classification**: Local instance (`localhost\SQLEXPRESS` or `(localdb)\MSSQLLocalDB`)
- **Database Classification**: Resettable Local Development environment
- **Database Name**: `TexasPortal_Dev` (configured in local launch configurations)
- **Environment**: Development
- **Shared / Isolated**: Isolated (local developer machine only)
- **Disposable / Persistent**: Resettable/Disposable
- **Resettable**: YES (via DB migration and `DevSeeder.SeedAsync` execution)
- **Contains Production Data**: NO
- **Contains Deterministic Test Data**: YES (populated by setting `SEED_DETERMINISTIC_DATA=true` on startup)
- **Safe for Test Writes**: YES
- **Safe for Destructive Reset**: YES

---

## 2. Test Execution Safeguards

To prevent developer errors from wiping shared database instances:
1. **Safety Assertions**: Teardown methods (`EnsureDeletedAsync`) are allowed **ONLY** if the connection string contains local indicators:
   - `localhost`
   - `127.0.0.1`
   - `SQLEXPRESS`
   - `(localdb)`
2. **Explicit Block**: If the host is remote (e.g. contains `.databaseasp.net`), any teardown command throws an immediate exception, preserving developer data.
