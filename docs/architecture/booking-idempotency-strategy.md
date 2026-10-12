# Booking Idempotency Design Strategy

To prevent double-booking charges during connection interruptions or retries, all booking write endpoints require database-backed idempotency keys.

---

## 1. Request Pipeline Flow

1. **Header Validation**: Middleware or filter intercepts the request and verifies the presence of the `Idempotency-Key` header (format: 36-char GUID).
2. **Hash Request**: Compute a SHA-256 hash of the request path + payload to bind the key to the specific request body.
3. **Database Check (Atomic Check-and-Insert)**:
   - Check if an `IdempotencyRecord` exists with the key + path.
   - If not found, insert a record with status `InProgress`.
   - If found with status `InProgress`, return `409 Conflict` (in-progress concurrency block).
   - If found with status `Completed` and same request hash, replay the cached response payload.
   - If found with status `Completed` but different request hash, return `400 Bad Request` (key reuse with different payload).
4. **Transaction Execute**: Execute the booking write inside an EF Core transaction scope.
5. **Mark Completed**: Upon successful commit, update the `IdempotencyRecord` status to `Completed`, saving the HTTP status code and response payload.

---

## 2. DB Schema Definition

```sql
CREATE TABLE [idempotency_records] (
    [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [idempotency_key] NVARCHAR(128) NOT NULL,
    [request_hash] NVARCHAR(256) NOT NULL,
    [status] NVARCHAR(64) NOT NULL, -- InProgress, Completed, Failed
    [response_code] INT NULL,
    [response_payload] NVARCHAR(MAX) NULL,
    [created_at] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [expires_at] DATETIME2 NOT NULL
);

-- Unique index to prevent concurrent race condition insertions
CREATE UNIQUE INDEX [IX_IdempotencyRecords_Key]
ON [idempotency_records] ([idempotency_key]);
```
- **Cleanup Strategy**: A lightweight background job can periodically purge expired records (e.g. older than 24 hours).
