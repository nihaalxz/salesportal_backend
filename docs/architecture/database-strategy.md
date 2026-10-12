# Database Strategy

This document details the database migration and architecture strategies for moving from the legacy MySQL database to the modernized Microsoft SQL Server backend.

---

## 1. Database Providers: Legacy vs. Target

### Legacy Database (Laravel)
- **Engine**: MySQL
- **Driver**: `mysql` (PDO-based driver in Laravel)
- **Port**: `3307` (Local/Development)
- **Charset / Collation**: `utf8mb4` with `utf8mb4_unicode_ci` (case-insensitive searches).
- **Timezone**: `Asia/Bahrain` (`APP_TIMEZONE` set in `.env`). Timestamps stored in local timezone or UTC depending on DB driver connection initialization.
- **Data Types**:
  - `decimal(10, 3)` / `decimal(15, 3)` for money.
  - `timestamp` / `datetime` for date-time values.
  - `tinyint(1)` for booleans.
  - Soft deletes managed via nullable `deleted_at` timestamps.

### Target Database (.NET Core Modernization)
- **Engine**: Microsoft SQL Server (MSSQL)
- **Provider**: `Microsoft.EntityFrameworkCore.SqlServer`
- **Connection String**: Mapped from standard configuration:
  `Server=db59295.public.databaseasp.net; Database=db59295; User Id=db59295; Password=***; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;`
- **Charset / Collation**: Default SQL Server collation (typically `SQL_Latin1_General_CP1_CI_AS` which is case-insensitive, accent-sensitive).
- **Timezone behavior**: We will map timestamps to `DateTime` (standard local or UTC depending on conventions) or `DateTimeOffset`. For legacy compatibility, dates will map to local `DateTime` in the `Asia/Bahrain` timezone, with plans to normalize new data to UTC.
- **Data Types**:
  - Money columns mapped to `decimal(15, 3)` exactly to match standard financial storage scale and precision.
  - Boolean values mapped to SQL Server `bit` type.
  - Date-only fields mapped to SQL Server `date`.
  - Time-only fields mapped to SQL Server `time`.

---

## 2. Timezone and DateTime Mapping Strategy

- **Laravel Timezone**: `Asia/Bahrain` (UTC+3).
- **.NET Strategy**:
  - Legacy `DateTime` fields will map directly to C# `DateTime` with `DateTimeKind.Unspecified` or explicit offset handling in the application layer.
  - Standardize all new API output formats to serialize to ISO-8601 strings with explicit offset indicators.
