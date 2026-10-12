# Date/Time Mapping Strategy

This document details legacy database timezone behaviors and defines the mapping rules for date/time columns during modernization.

---

## 1. Legacy MySQL Timezone Context

1. **MySQL Types**:
   - **`timestamp`**: Timezone-aware. Stored in UTC and converted to the database connection's session timezone upon retrieval.
   - **`datetime` / `date`**: Timezone-naive. Stored and retrieved exactly as written.
2. **Laravel Configuration**:
   - `APP_TIMEZONE` is set to `Asia/Bahrain` (UTC+3) in `.env`.
   - Laravel PDO session initializations set the connection timezone to `Asia/Bahrain`. Therefore, all queries store/retrieve values in the Bahrain offset.
3. **Eloquent Serialization**:
   - Carbon instances serialize dates into ISO-8601 strings, converting them to the local `Asia/Bahrain` time offset (e.g. `2026-07-10T18:00:00+03:00`).

---

## 2. Target .NET Strategy

To prevent date shifts when migrating records or running concurrent backends:

| Category | Source MySQL Column | Target SQL Server Type | C# CLR Type | Read/Write Policy |
| :--- | :--- | :--- | :--- | :--- |
| **System Audits** | `created_at`, `updated_at` | `datetime2` | `DateTime` | Read as local `DateTimeKind.Unspecified`. When serializing to API clients, format with explicit `+03:00` offset to maintain legacy compatibility. |
| **Business Dates** | `check_in_date`, `drop_date` | `date` | `DateTime` (DateOnly) | Read-only Date values (ignore time). Mapped to SQL Server `date`. |
| **Business Times** | `appointment_time`, `drop_time` | `time` | `TimeSpan` | Store time duration since midnight. |
| **Soft Deletes** | `deleted_at` | `datetime2` | `DateTime?` | Mapped as nullable `DateTime?`. |

---

## 3. Coexistence & DST Constraints
* **Bahrain Timezone**: Bahrain does not observe Daylight Saving Time (DST). It remains UTC+3 year-round. This simplifies conversions: a constant +3 hour offset can be applied programmatically when parsing legacy values.
