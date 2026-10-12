# Booking Identifier Generation Strategy

This document details the strategy for generating unique and collision-free business identifiers for bookings.

---

## 1. Chosen Strategy: Database Identity Key

- **Column**: `id` (configured as an auto-incrementing IDENTITY column in SQL Server).
- **Concurrency Safety**: SQL Server database identity assignment is inherently concurrent-safe, thread-safe, and fully rollback-safe.
- **Prefix Column**: The prefix column (`prefix`) is supplied optionally from request metadata to record external transaction prefix codes.
- **Avoided Weaknesses**: We strictly avoid any code-level generation algorithms like `MAX(id) + 1` or counting current records, which suffer from dirty reads and severe concurrency collisions under concurrent API requests.
